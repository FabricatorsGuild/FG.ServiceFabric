using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace FG.ServiceFabric.Services.Runtime.State
{
    public class SessionCommittedEventArgs : EventArgs
    {
        public SessionCommittedEventArgs(IReadOnlyCollection<ReliableStateChange> stateChanges)
        {
            StateChanges = stateChanges;
        }

        public IReadOnlyCollection<ReliableStateChange> StateChanges { get; }
    }

    public class ReliableStateStatefulServiceStateManagerSession : IStatefulServiceStateManagerSession, IDisposable
    {
        private readonly IReliableStateManager _innerStateManagerReplica;

        private readonly ConcurrentDictionary<string, object> _reliableStatesOpen;
        private readonly IList<ReliableStateChange> _stateChanges;

        private bool _isAborted;
        private bool _isCommitted;
        private bool _isOpen;
        private ITransaction _transaction;

        public ReliableStateStatefulServiceStateManagerSession(IReliableStateManager innerStateManagerReplica)
        {
            _innerStateManagerReplica = innerStateManagerReplica;

            _isAborted = false;
            _isCommitted = false;
            _isOpen = false;

            _reliableStatesOpen = new ConcurrentDictionary<string, object>();

            _stateChanges = new List<ReliableStateChange>();
        }

        public IReadOnlyCollection<ReliableStateChange> StateChanges =>
            new ReadOnlyCollection<ReliableStateChange>(_stateChanges);

        public async Task<IStatefulServiceStateManagerSession> ForDictionary<T>(string schema)
        {
            await GetOrCreateReliableDictionaryAsync<T>(schema);
            return this;
        }

        public async Task<IStatefulServiceStateManagerSession> ForQueue<T>(string schema)
        {
            await GetOrCreateReliableQueue<T>(schema);
            return this;
        }


        // Summary:
        // Occurs when State Manager's session is committed
        public event EventHandler<SessionCommittedEventArgs> SessionCommitted;

        public void Dispose()
        {
            if (!_isAborted)
                if (!_isCommitted)
                    CommitInteralAsync().GetAwaiter().GetResult();

            _transaction?.Dispose();
        }

        public async Task SetAsync<T>(string schema, string storageKey, T value)
        {
            var reliableDictionary = await GetOrCreateReliableDictionaryAsync<T>(schema);

            await reliableDictionary.SetAsync(GetTransaction(), storageKey, value);

            _stateChanges.Add(new ReliableStateChange(schema, storageKey, typeof(T), value,
                ReliableStateChangeKind.AddOrUpdate));
        }

        public async Task<T> GetOrAddAsync<T>(string schema, string storageKey, Func<string, T> newValue)
        {
            var reliableDictionary = await GetOrCreateReliableDictionaryAsync<T>(schema);

            var exists = true;
            var value = await reliableDictionary.GetOrAddAsync(
                GetTransaction(), storageKey, key =>
                {
                    exists = false;
                    return newValue(key);
                });

            if (!exists)
                _stateChanges.Add(new ReliableStateChange(schema, storageKey, typeof(T), value,
                    ReliableStateChangeKind.Add));

            return value;
        }

        public async Task<ConditionalValue<T>> TryGetAsync<T>(string schema, string storageKey)
        {
            var reliableDictionary = await GetOrCreateReliableDictionaryAsync<T>(schema);

            var value = await reliableDictionary.TryGetValueAsync(GetTransaction(), storageKey);
            return value;
        }

        public async Task RemoveAsync<T>(string schema, string storageKey)
        {
            var reliableDictionary = await GetReliableDictionaryAsync<T>(schema);

            if (reliableDictionary != null)
            {
                var exists = await reliableDictionary.TryRemoveAsync(GetTransaction(), storageKey);
                if (exists.HasValue)
                    _stateChanges.Add(new ReliableStateChange(schema, storageKey, typeof(T), exists.Value,
                        ReliableStateChangeKind.Remove));
            }
        }

        public async Task EnqueueAsync<T>(string schema, T value)
        {
            var reliableQueue = await GetOrCreateReliableQueue<T>(schema);

            await reliableQueue.EnqueueAsync(GetTransaction(), value);

            _stateChanges.Add(new ReliableStateChange(schema, typeof(T), value, ReliableStateChangeKind.Enqueue));
        }

        public async Task<ConditionalValue<T>> DequeueAsync<T>(string schema)
        {
            var reliableQueue = await GetOrCreateReliableQueue<T>(schema);

            var value = await reliableQueue.TryDequeueAsync(GetTransaction());

            _stateChanges.Add(new ReliableStateChange(schema, typeof(T), value, ReliableStateChangeKind.Dequeue));
            return value;
        }

        public async Task<ConditionalValue<T>> PeekAsync<T>(string schema)
        {
            var reliableQueue = await GetOrCreateReliableQueue<T>(schema);
            var value = await reliableQueue.TryPeekAsync(GetTransaction());
            return value;
        }

        public async Task CommitAsync()
        {
            await CommitInteralAsync();
        }

        public Task AbortAsync()
        {
            _transaction?.Abort();
            _isAborted = true;

            _isOpen = false;
            return Task.FromResult(true);
        }

        public async Task<IEnumerable<KeyValuePair<string, T>>> EnumerateDictionary<T>(string schema)
        {
            CheckIsOpen();

            var reliableQueue = await GetOrCreateReliableDictionaryAsync<T>(schema);
            var results = new List<KeyValuePair<string, T>>();

            var asyncEnumerable = await reliableQueue.CreateEnumerableAsync(GetTransaction());
            var enumerator = asyncEnumerable.GetAsyncEnumerator();

            while (await enumerator.MoveNextAsync(CancellationToken.None))
                results.Add(enumerator.Current);

            return results;
        }

        public async Task<IEnumerable<T>> EnumerateQueue<T>(string schema)
        {
            CheckIsOpen();

            var reliableQueue = await GetOrCreateReliableQueue<T>(schema);
            var results = new List<T>();

            var asyncEnumerable = await reliableQueue.CreateEnumerableAsync(GetTransaction());
            var enumerator = asyncEnumerable.GetAsyncEnumerator();

            while (await enumerator.MoveNextAsync(CancellationToken.None))
                results.Add(enumerator.Current);

            return results;
        }

        private void CheckIsOpen()
        {
            if (!_isOpen)
                throw new ReliableStateStatefulServiceStateManagerSessionException(
                    $"The session (and underlying transaction) {(_isCommitted ? "has been Committed" : (_isAborted ? "has been Aborted" : " is not open for unknown reason"))} ");
        }

        private async Task CommitInteralAsync()
        {
            await _transaction?.CommitAsync();
            _isCommitted = true;

            _isOpen = false;
            SessionCommitted?.Invoke(this, new SessionCommittedEventArgs(StateChanges));
        }

        private ITransaction GetTransaction()
        {
            if (_transaction == null)
            {
                _transaction = _innerStateManagerReplica.CreateTransaction();
                _isOpen = true;
            }

            return _transaction;
        }


        private async Task<IReliableDictionary2<string, T>> GetOrCreateReliableDictionaryAsync<T>(string schema)
        {
            if (_reliableStatesOpen.TryGetValue(schema, out var reliableDictionary))
                return (IReliableDictionary2<string, T>) reliableDictionary;
            // Dont create the dictionary in the transaction, see https://github.com/Azure/service-fabric-issues/issues/24
            reliableDictionary = await _innerStateManagerReplica.GetOrAddAsync<IReliableDictionary2<string, T>>(schema);
            _reliableStatesOpen.TryAdd(schema, reliableDictionary);

            return null;
        }

        private async Task<IReliableDictionary2<string, T>> GetReliableDictionaryAsync<T>(string schema)
        {
            if (_reliableStatesOpen.TryGetValue(schema, out var reliableDictionary))
            {
                return (IReliableDictionary2<string, T>) reliableDictionary;
            }
            // Dont create the dictionary in the transaction, see https://github.com/Azure/service-fabric-issues/issues/24
            var reliableDictionaryValue =
                await _innerStateManagerReplica.TryGetAsync<IReliableDictionary2<string, T>>(schema);
            if (reliableDictionaryValue.HasValue)
            {
                reliableDictionary = reliableDictionaryValue.Value;
                _reliableStatesOpen.TryAdd(schema, reliableDictionary);
            }

            return null;
        }

        private async Task<IReliableQueue<T>> GetOrCreateReliableQueue<T>(string schema)
        {
            if (_reliableStatesOpen.TryGetValue(schema, out var reliableQueue))
                return (IReliableQueue<T>) reliableQueue;

            // Dont create the queue in the transaction, see https://github.com/Azure/service-fabric-issues/issues/24
            reliableQueue = await _innerStateManagerReplica.GetOrAddAsync<IReliableQueue<T>>(schema);
            _reliableStatesOpen.TryAdd(schema, reliableQueue);

            return (IReliableQueue<T>) reliableQueue;
        }
    }
}