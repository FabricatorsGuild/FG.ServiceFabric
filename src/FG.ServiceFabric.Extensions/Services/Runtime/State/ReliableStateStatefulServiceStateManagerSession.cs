using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
            this.StateChanges = stateChanges;
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
            this._innerStateManagerReplica = innerStateManagerReplica;

            this._isAborted = false;
            this._isCommitted = false;
            this._isOpen = false;

            this._reliableStatesOpen = new ConcurrentDictionary<string, object>();

            this._stateChanges = new List<ReliableStateChange>();
        }

        public IReadOnlyCollection<ReliableStateChange> StateChanges =>
            new ReadOnlyCollection<ReliableStateChange>(this._stateChanges);

        public async Task<IStatefulServiceStateManagerSession> ForDictionary<T>(string schema)
        {
            await this.GetOrCreateReliableDictionaryAsync<T>(schema);
            return this;
        }

        public async Task<IStatefulServiceStateManagerSession> ForQueue<T>(string schema)
        {
            await this.GetOrCreateReliableQueue<T>(schema);
            return this;
        }


        // Summary:
        // Occurs when State Manager's session is committed
        public event EventHandler<SessionCommittedEventArgs> SessionCommitted;

        public void Dispose()
        {
            if (!this._isAborted)
            {
                if (!this._isCommitted)
                {
                    this.CommitInteralAsync().GetAwaiter().GetResult();
                }
            }

            this._transaction?.Dispose();
        }

        public async Task SetAsync<T>(string schema, string storageKey, T value)
        {
            var reliableDictionary = await this.GetOrCreateReliableDictionaryAsync<T>(schema);

            await reliableDictionary.SetAsync(this.GetTransaction(), storageKey, value);

            this._stateChanges.Add(new ReliableStateChange(schema, storageKey, typeof(T), value,
                ReliableStateChangeKind.AddOrUpdate));
        }

        public async Task<T> GetOrAddAsync<T>(string schema, string storageKey, Func<string, T> newValue)
        {
            var reliableDictionary = await this.GetOrCreateReliableDictionaryAsync<T>(schema);

            var exists = true;
            var value = await reliableDictionary.GetOrAddAsync(
                            this.GetTransaction(), storageKey, (key) =>
            {
                exists = false;
                return newValue(key);
            });

            if (!exists)
            {
                this._stateChanges.Add(new ReliableStateChange(schema, storageKey, typeof(T), value, ReliableStateChangeKind.Add));
            }

            return value;
        }

        public async Task<ConditionalValue<T>> TryGetAsync<T>(string schema, string storageKey)
        {
            var reliableDictionary = await this.GetOrCreateReliableDictionaryAsync<T>(schema);

            var value = await reliableDictionary.TryGetValueAsync(this.GetTransaction(), storageKey);
            return value;
        }

        public async Task RemoveAsync<T>(string schema, string storageKey)
        {
            var reliableDictionary = await this.GetReliableDictionaryAsync<T>(schema);

            if (reliableDictionary != null)
            {
                var exists = await reliableDictionary.TryRemoveAsync(this.GetTransaction(), storageKey);
                if (exists.HasValue)
                {
                    this._stateChanges.Add(new ReliableStateChange(schema, storageKey, typeof(T), exists.Value,
                        ReliableStateChangeKind.Remove));
                }
            }
        }

        public async Task EnqueueAsync<T>(string schema, T value)
        {
            var reliableQueue = await this.GetOrCreateReliableQueue<T>(schema);

            await reliableQueue.EnqueueAsync(this.GetTransaction(), value);

            this._stateChanges.Add(new ReliableStateChange(schema, typeof(T), value, ReliableStateChangeKind.Enqueue));
        }

        public async Task<ConditionalValue<T>> DequeueAsync<T>(string schema)
        {
            var reliableQueue = await this.GetOrCreateReliableQueue<T>(schema);

            var value = await reliableQueue.TryDequeueAsync(this.GetTransaction());

            this._stateChanges.Add(new ReliableStateChange(schema, typeof(T), value, ReliableStateChangeKind.Dequeue));
            return value;
        }

        public async Task<ConditionalValue<T>> PeekAsync<T>(string schema)
        {
            var reliableQueue = await this.GetOrCreateReliableQueue<T>(schema);
            var value = await reliableQueue.TryPeekAsync(this.GetTransaction());
            return value;
        }

        public async Task CommitAsync()
        {
            await this.CommitInteralAsync();
        }

        public Task AbortAsync()
        {
            this._transaction?.Abort();
            this._isAborted = true;

            this._isOpen = false;
            return Task.FromResult(true);
        }

        public async Task<IEnumerable<KeyValuePair<string, T>>> EnumerateDictionary<T>(string schema)
        {
            this.CheckIsOpen();

            var reliableQueue = await this.GetOrCreateReliableDictionaryAsync<T>(schema);
            var results = new List<KeyValuePair<string, T>>();

            var asyncEnumerable = await reliableQueue.CreateEnumerableAsync(this.GetTransaction());
            var enumerator = asyncEnumerable.GetAsyncEnumerator();

            while (await enumerator.MoveNextAsync(CancellationToken.None))
            {
                results.Add(enumerator.Current);
            }

            return results;
        }

        public async Task<IEnumerable<T>> EnumerateQueue<T>(string schema)
        {
            this.CheckIsOpen();

            var reliableQueue = await this.GetOrCreateReliableQueue<T>(schema);
            var results = new List<T>();

            var asyncEnumerable = await reliableQueue.CreateEnumerableAsync(this.GetTransaction());
            var enumerator = asyncEnumerable.GetAsyncEnumerator();

            while (await enumerator.MoveNextAsync(CancellationToken.None))
            {
                results.Add(enumerator.Current);
            }

            return results;
        }

        private void CheckIsOpen()
        {
            if (!this._isOpen)
            {
                throw new ReliableStateStatefulServiceStateManagerSessionException(
                    $"The session (and underlying transaction) {(this._isCommitted ? "has been Committed" : (this._isAborted ? "has been Aborted" : " is not open for unknown reason"))} ") ;
            }
        }

        private async Task CommitInteralAsync()
        {
            await this._transaction?.CommitAsync();
            this._isCommitted = true;

            this._isOpen = false;
            this.SessionCommitted?.Invoke(this, new SessionCommittedEventArgs(this.StateChanges));
        }

        private ITransaction GetTransaction()
        {
            if (this._transaction == null)
            {
                this._transaction = this._innerStateManagerReplica.CreateTransaction();
                this._isOpen = true;
            }

            return this._transaction;
        }


        private async Task<IReliableDictionary2<string, T>> GetOrCreateReliableDictionaryAsync<T>(string schema)
        {
            if (this._reliableStatesOpen.TryGetValue(schema, out var reliableDictionary))
            {
                return (IReliableDictionary2<string, T>)reliableDictionary;
            }
            else
            {
                // Dont create the dictionary in the transaction, see https://github.com/Azure/service-fabric-issues/issues/24
                reliableDictionary = await this._innerStateManagerReplica.GetOrAddAsync<IReliableDictionary2<string, T>>(schema);
                this._reliableStatesOpen.TryAdd(schema, reliableDictionary);
            }

            return null;
        }

        private async Task<IReliableDictionary2<string, T>> GetReliableDictionaryAsync<T>(string schema)
        {
            if (this._reliableStatesOpen.TryGetValue(schema, out var reliableDictionary))
            {
                return (IReliableDictionary2<string, T>)reliableDictionary;
            }
            else
            {
                // Dont create the dictionary in the transaction, see https://github.com/Azure/service-fabric-issues/issues/24
                var reliableDictionaryValue = await this._innerStateManagerReplica.TryGetAsync<IReliableDictionary2<string, T>>(schema);
                if (reliableDictionaryValue.HasValue)
                {
                    reliableDictionary = reliableDictionaryValue.Value;
                    this._reliableStatesOpen.TryAdd(schema, reliableDictionary);
                }
            }

            return null;
        }

        private async Task<IReliableQueue<T>> GetOrCreateReliableQueue<T>(string schema)
        {
            if (this._reliableStatesOpen.TryGetValue(schema, out var reliableQueue))
            {
                return (IReliableQueue<T>)reliableQueue;
            }

            // Dont create the queue in the transaction, see https://github.com/Azure/service-fabric-issues/issues/24
            reliableQueue = await this._innerStateManagerReplica.GetOrAddAsync<IReliableQueue<T>>(schema);
            this._reliableStatesOpen.TryAdd(schema, reliableQueue);

            return (IReliableQueue<T>)reliableQueue;
        }
    }
}