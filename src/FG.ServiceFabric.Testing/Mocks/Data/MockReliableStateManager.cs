﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Testing.Mocks.Data.Collections;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Data.Notifications;

namespace FG.ServiceFabric.Testing.Mocks.Data
{
    public class MockReliableStateManager : IReliableStateManagerReplica
    {
        private readonly ConcurrentDictionary<Uri, IReliableState> _store = new ConcurrentDictionary<Uri, IReliableState>();
        private Func<CancellationToken, Task<bool>> _datalossFunction;

        private readonly Dictionary<Type, Type> _dependencyMap = new Dictionary<Type, Type>()
        {
            {typeof(IReliableDictionary<,>), typeof(MockReliableDictionary<,>)},
            {typeof(IReliableQueue<>), typeof(MockReliableQueue<>)}
        };

        public Func<CancellationToken, Task<bool>> OnDataLossAsync
        {
            set
            {
                this._datalossFunction = value;
            }

            get
            {
                return this._datalossFunction;
            }
        }

#pragma warning disable 0067
        public event EventHandler<NotifyTransactionChangedEventArgs> TransactionChanged;
        public event EventHandler<NotifyStateManagerChangedEventArgs> StateManagerChanged;
#pragma warning restore 0067

        public Task ClearAsync(ITransaction tx)
        {
            this._store.Clear();
            return Task.FromResult(true);
        }

        public Task ClearAsync()
        {
            this._store.Clear();
            return Task.FromResult(true);
        }

        public ITransaction CreateTransaction()
        {
            return new MockTransaction();
        }

        public Task RemoveAsync(string name)
        {
            IReliableState result;
            this._store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, string name)
        {
            IReliableState result;
            this._store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(string name, TimeSpan timeout)
        {
            IReliableState result;
            this._store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, string name, TimeSpan timeout)
        {
            IReliableState result;
            this._store.TryRemove(this.ToUri(name), out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(Uri name)
        {
            IReliableState result;
            this._store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(Uri name, TimeSpan timeout)
        {
            IReliableState result;
            this._store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, Uri name)
        {
            IReliableState result;
            this._store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task RemoveAsync(ITransaction tx, Uri name, TimeSpan timeout)
        {
            IReliableState result;
            this._store.TryRemove(name, out result);

            return Task.FromResult(true);
        }

        public Task<ConditionalValue<T>> TryGetAsync<T>(string name) where T : IReliableState
        {
            IReliableState item;
            bool result = this._store.TryGetValue(this.ToUri(name), out item);

            return Task.FromResult(new ConditionalValue<T>(result, (T)item));
        }

        public Task<ConditionalValue<T>> TryGetAsync<T>(Uri name) where T : IReliableState
        {
            IReliableState item;
            bool result = this._store.TryGetValue(name, out item);

            return Task.FromResult(new ConditionalValue<T>(result, (T)item));
        }

        public Task<T> GetOrAddAsync<T>(string name) where T : IReliableState
        {
            return Task.FromResult((T)this._store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, string name) where T : IReliableState
        {
            return Task.FromResult((T)this._store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(string name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T)this._store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, string name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T)this._store.GetOrAdd(this.ToUri(name), this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(Uri name) where T : IReliableState
        {
            return Task.FromResult((T)this._store.GetOrAdd(name, this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(Uri name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T)this._store.GetOrAdd(name, this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name) where T : IReliableState
        {
            return Task.FromResult((T)this._store.GetOrAdd(name, this.GetDependency(typeof(T))));
        }

        public Task<T> GetOrAddAsync<T>(ITransaction tx, Uri name, TimeSpan timeout) where T : IReliableState
        {
            return Task.FromResult((T)this._store.GetOrAdd(name, this.GetDependency(typeof(T))));
        }

        public bool TryAddStateSerializer<T>(IStateSerializer<T> stateSerializer)
        {
            throw new NotImplementedException();
        }

        private IReliableState GetDependency(Type t)
        {
            Type mockType = this._dependencyMap[t.GetGenericTypeDefinition()];

            return (IReliableState)Activator.CreateInstance(mockType.MakeGenericType(t.GetGenericArguments()));
        }

        private Uri ToUri(string name)
        {
            return new Uri("mock://" + name, UriKind.Absolute);
        }

        public IAsyncEnumerator<IReliableState> GetAsyncEnumerator()
        {
            return new MockAsyncEnumerator<IReliableState>(this._store.Values.GetEnumerator());
        }

        public void Initialize(StatefulServiceInitializationParameters initializationParameters)
        {

        }

        public Task<IReplicator> OpenAsync(ReplicaOpenMode openMode, IStatefulServicePartition partition, CancellationToken cancellationToken)
        {
            return null;
        }

        public Task ChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public void Abort()
        {
        }

        public Task BackupAsync(Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            throw new NotImplementedException();
        }

        public Task BackupAsync(BackupOption option, TimeSpan timeout, CancellationToken cancellationToken, Func<BackupInfo, CancellationToken, Task<bool>> backupCallback)
        {
            throw new NotImplementedException();
        }

        public Task RestoreAsync(string backupFolderPath)
        {
            throw new NotImplementedException();
        }

        public Task RestoreAsync(string backupFolderPath, RestorePolicy restorePolicy, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}