using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession.Internal
{
    using Nito.AsyncEx;

    public abstract class StateSessionManagerBase<TStateSession> : IStateSessionManager, IStateSessionWritableManager, IStateSessionManagerInternals
        where TStateSession : class, IStateSession
    {
        public class StateChange
        {
            public StateChange(StateChangeType changeType, StateWrapper value, Type valueType)
            {
                this.ChangeType = changeType;
                this.Value = value;
                this.ValueType = valueType;
            }

            public StateChangeType ChangeType { get; set; }
            public StateWrapper Value { get; set; }
            public Type ValueType { get; set; }
        }

        public enum StateChangeType
        {
            None = 0,
            AddOrUpdate = 1,
            Remove = 2,
            Enqueue = 3,
            Dequeue = 4,
        }

        private readonly ConcurrentDictionary<string, QueueInfo> _openQueues = new ConcurrentDictionary<string, QueueInfo>();

        private readonly AsyncReaderWriterLock _lock = new Nito.AsyncEx.AsyncReaderWriterLock();

        AsyncReaderWriterLock IStateSessionManagerInternals.Lock => this._lock;

        protected StateSessionManagerBase(
            string serviceName,
            Guid partitionId,
            string partitionKey)
        {
            this.ServiceName = serviceName;
            this.PartitionId = partitionId;
            this.PartitionKey = partitionKey;

		    _storagePartitionKey = HashUtil.Adler32String($"{serviceName}-{partitionKey}");
        }

        protected Guid PartitionId { get; }
        protected string PartitionKey { get; }
        protected string ServiceName { get; }

	    private readonly string _storagePartitionKey;

        #region Internals

        private IStateSessionManagerInternals Internals => (IStateSessionManagerInternals)this;

        IDictionary<string, QueueInfo> IStateSessionManagerInternals.OpenQueues => this._openQueues;

        SchemaStateKey IStateSessionManagerInternals.GetKey(ISchemaKey key)
        {
            return new SchemaStateKey(this.ServiceName, this.PartitionKey, key?.Schema, key?.Key);
        }

        IServiceMetadata IStateSessionManagerInternals.GetMetadata()
        {
			return new ServiceMetadata() { ServiceName = ServiceName, ServicePartitionKey = PartitionKey, StoragePartitionKey = GetStoragePartitionKey(ServiceName, PartitionKey)};
		}

	    protected virtual string GetStoragePartitionKey(string serviceName, string partitionKey)
	    {
	        return _storagePartitionKey;
        }

        IValueMetadata IStateSessionManagerInternals.GetOrCreateMetadata(IValueMetadata metadata, StateWrapperType type)
        {
            if (metadata == null)
            {
                metadata = new ValueMetadata(type);
            }
            else
            {
			    if (metadata.Type == null)
			    {
			        metadata.SetType(type);
			    }
			}
            return metadata;
        }

        StateWrapper IStateSessionManagerInternals.BuildWrapper(IValueMetadata valueMetadata, SchemaStateKey key)
        {
            var serviceMetadata = this.Internals.GetMetadata();
            if (valueMetadata == null) valueMetadata = new ValueMetadata(StateWrapperType.Unknown);
            valueMetadata.Key = key.Key;
            valueMetadata.Schema = key.Schema;
            var wrapper = valueMetadata.BuildStateWrapper(key, serviceMetadata);
            return wrapper;
        }

        StateWrapper IStateSessionManagerInternals.BuildWrapper(IValueMetadata metadata, SchemaStateKey key,
            Type valueType, object value)
        {
            return (StateWrapper)this.CallGenericMethod(
                $"{typeof(IStateSessionManagerInternals).FullName}.{nameof(IStateSessionManagerInternals.BuildWrapperGeneric)}",
                new[] { valueType },
                this.Internals.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), key, value);
        }

        StateWrapper<T> IStateSessionManagerInternals.BuildWrapperGeneric<T>(IValueMetadata valueMetadata, SchemaStateKey key, T value)
        {
            var serviceMetadata = this.Internals.GetMetadata();
            if (valueMetadata == null) valueMetadata = new ValueMetadata(StateWrapperType.Unknown);
            valueMetadata.Key = key.Key;
			valueMetadata.Schema = key.Schema;            
            var wrapper = valueMetadata.BuildStateWrapper(key, value, serviceMetadata);
            return wrapper;
        }

        string IStateSessionManagerInternals.GetEscapedKey(string key)
        {
            return this.GetEscapedKeyInternal(key);
        }

        string IStateSessionManagerInternals.GetUnescapedKey(string key)
        {
            return this.GetUnescapedKeyInternal(key);
        }

        #endregion

        public Task<IStateSessionReadOnlyDictionary<T>> OpenDictionary<T>(string schema,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var dictionary =
                (IStateSessionReadOnlyDictionary<T>)
                new StateSessionBaseReadOnlyDictionary<StateSessionManagerBase<TStateSession>, TStateSession, T>(this, schema, readOnly: false);
            return Task.FromResult(dictionary);
        }

        Task<IStateSessionDictionary<T>> IStateSessionWritableManager.OpenDictionary<T>(
            string schema,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var dictionary =
                (IStateSessionDictionary<T>)
                new StateSessionBaseDictionary<StateSessionManagerBase<TStateSession>, TStateSession, T>(this, schema, readOnly: false);
            return Task.FromResult(dictionary);
        }

        Task<IStateSessionQueue<T>> IStateSessionWritableManager.OpenQueue<T>(
            string schema,
            CancellationToken cancellationToken = new CancellationToken())
        {
            this._openQueues.TryAdd(schema, null);

            var queue = (IStateSessionQueue<T>)new StateSessionBaseQueue<TStateSession, T>(this, schema, readOnly: false);
            return Task.FromResult(queue);
        }

        public Task<IStateSessionReadOnlyQueue<T>> OpenQueue<T>(
            string schema,
            CancellationToken cancellationToken = new CancellationToken())
        {
            this._openQueues.TryAdd(schema, null);
            var queue = (IStateSessionReadOnlyQueue<T>)new StateSessionBaseReadOnlyQueue<TStateSession, T>(this, schema, readOnly: false);
            return Task.FromResult(queue);
        }

        #region  Writable
        public IStateSessionWritableManager Writable => (IStateSessionWritableManager)this;
        IStateSession IStateSessionWritableManager.CreateSession(params IStateSessionObject[] stateSessionObjects)
        {
            var session = this.CreateSessionInternal(this, stateSessionObjects);
            return session;
        }

        #endregion

        public IStateSessionReader CreateSession(params IStateSessionReadOnlyObject[] stateSessionObjects)
        {
            var session = this.CreateSessionInternal(this, stateSessionObjects);
            return session;
        }

        protected virtual string GetEscapedKeyInternal(string key)
        {
            return key;
        }

        protected virtual string GetUnescapedKeyInternal(string key)
        {
            return key;
        }

        protected abstract TStateSession CreateSessionInternal(StateSessionManagerBase<TStateSession> manager,
            IStateSessionReadOnlyObject[] stateSessionObjects);
        protected abstract TStateSession CreateSessionInternal(StateSessionManagerBase<TStateSession> manager,
            IStateSessionObject[] stateSessionObjects);


        public abstract class StateSessionBase<TStateSessionManager> : IStateSession
            where TStateSessionManager : class, IStateSessionManagerInternals, IStateSessionManager
        {
            private readonly object _lock = new object();
            private readonly TStateSessionManager _manager;

            private readonly IDictionary<string, StateChange> _transactedChanges =
                new ConcurrentDictionary<string, StateChange>();

            private IEnumerable<IStateSessionReadOnlyObject> _attachedObjects;

            private IDisposable _rwLock;

            protected StateSessionBase(TStateSessionManager manager, IEnumerable<IStateSessionReadOnlyObject> stateSessionObjects)
            {
                this.AttachObjects(stateSessionObjects);
                this._manager = manager;

                this.IsReadOnly = true;
                this.AquireLock().GetAwaiter().GetResult();
            }

            protected StateSessionBase(TStateSessionManager manager, IEnumerable<IStateSessionObject> stateSessionObjects)
            {
                this.AttachObjects(stateSessionObjects);
                this._manager = manager;

                this.IsReadOnly = false;
                this.AquireLock().GetAwaiter().GetResult();
            }

            private async Task AquireLock()
            {
                this._rwLock = this.IsReadOnly ? await this._managerInternals.Lock.ReaderLockAsync() : await this._managerInternals.Lock.WriterLockAsync();
            }

            public bool IsReadOnly { get; }

            private void CheckIsNotReadOnly()
            {
                if (this.IsReadOnly)
                {
                    throw new StateSessionException($"Tried to modify a StateSession that is in ReadOnly mode");
                }
            }

            private IStateSessionManagerInternals _managerInternals => this._manager;

            public Task<bool> Contains<T>(string schema, string key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                return this.Contains(schema, key, cancellationToken);
            }

            public Task<bool> Contains(string schema, string key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var id = this._managerInternals.GetKey(new DictionaryStateKey(schema, key));

                if (this._transactedChanges.TryGetValue(id, out var transactedChange))
                {
                    // Check if session contains it, or if it has been removed from session
                    switch (transactedChange.ChangeType)
                    {
                        case StateChangeType.AddOrUpdate:
                            return Task.FromResult(true);
                        case StateChangeType.Remove:
                            return Task.FromResult(false);
                        case StateChangeType.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
                return this.ContainsInternal(id, cancellationToken);
            }

            public Task<FindByKeyPrefixResult<T>> FindByKeyPrefixAsync<T>(string schema, string keyPrefix,
                int maxNumResults = 100000,
                ContinuationToken continuationToken = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotImplementedException();
            }

            public async Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string schema, string keyPrefix,
                int maxNumResults = 100000,
                ContinuationToken continuationToken = null,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var schemaKeyPrefix = this._managerInternals.GetKey(new DictionaryStateKey(schema, this._manager.GetEscapedKey(keyPrefix)));
                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

                var result =
                    await this.FindByKeyPrefixInternalAsync(schemaKeyPrefix, maxNumResults, continuationToken, cancellationToken);
                return result;
            }

            public async Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var schemaKeyPrefix = this._manager.GetKey(null);
                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
                var result = await this.EnumerateSchemaNamesInternalAsync(schemaKeyPrefix, key, cancellationToken);
                return result;
            }

            public async Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var id = this._managerInternals.GetKey(new DictionaryStateKey(schema, this._manager.GetEscapedKey(key)));

                // Check if session contains it, or if it has been removed from session
                if (this._transactedChanges.TryGetValue(id, out var transactedChange))
                {
                    switch (transactedChange.ChangeType)
                    {
                        case StateChangeType.AddOrUpdate:
                            return new ConditionalValue<T>(true, ((StateWrapper<T>)transactedChange.Value).State);
                        case StateChangeType.Remove:
                            return new ConditionalValue<T>(false, default(T));
                        case StateChangeType.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
                var value = await this.TryGetValueInternalAsync<T>(id, cancellationToken);
                if (value.HasValue)
                {
                    return new ConditionalValue<T>(true, value.Value.State);
                }

                return new ConditionalValue<T>(false, default(T));
            }

            public async Task<T> GetValueAsync<T>(string schema, string key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var id = this._managerInternals.GetKey(new DictionaryStateKey(schema, this._manager.GetEscapedKey(key)));

                // Check if session contains it, or if it has been removed from session
                if (this._transactedChanges.TryGetValue(id, out var transactedChange))
                {
                    switch (transactedChange.ChangeType)
                    {
                        case StateChangeType.AddOrUpdate:
                            return ((StateWrapper<T>)transactedChange.Value).State;
                        case StateChangeType.Remove:
                            throw new KeyNotFoundException($"State with {id} does not exist");
                        case StateChangeType.None:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
                var value = await this.GetValueInternalAsync<T>(id, cancellationToken);
                if (value != null)
                {
                    return value.State;
                }

                throw new StateSessionException($"Failed to GetValueAsync for {schema}:{key}");
            }

            public Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                this.CheckIsNotReadOnly();

                var id = this._managerInternals.GetKey(new DictionaryStateKey(schema, this._manager.GetEscapedKey(key)));

                var valueType = typeof(T);
                var document = this._managerInternals.BuildWrapperGeneric<T>(this._managerInternals.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id, value);

                this._transactedChanges[id] = new StateChange(StateChangeType.AddOrUpdate, document, valueType);
                return Task.FromResult(true);
            }

            public Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metadata,
                CancellationToken cancellationToken = new CancellationToken())
            {
                this.CheckIsNotReadOnly();

                var id = this._managerInternals.GetKey(new DictionaryStateKey(schema, this._manager.GetEscapedKey(key)));
                var document = this._managerInternals.BuildWrapper(
                    this._managerInternals.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id,
                    valueType, value);

                this._transactedChanges[id] = new StateChange(StateChangeType.AddOrUpdate, document, valueType);
                return Task.FromResult(true);
            }

            public Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
            {
                return this.RemoveAsync(schema, key, cancellationToken);
            }

            public Task RemoveAsync(string schema, string key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                this.CheckIsNotReadOnly();

                var id = this._managerInternals.GetKey(new DictionaryStateKey(schema, this._manager.GetEscapedKey(key)));
                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

                var stateWrapper = this._managerInternals.BuildWrapper(this._managerInternals.GetOrCreateMetadata(null, StateWrapperType.ReliableDictionaryItem), id);

                this._transactedChanges[id] = new StateChange(StateChangeType.Remove, stateWrapper, null);
                return Task.FromResult(true);
            }

            public async Task EnqueueAsync<T>(string schema, T value, IValueMetadata metadata,
                CancellationToken cancellationToken = new CancellationToken())
            {
                this.CheckIsNotReadOnly();

                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

                var stateQueueInfo = await this.GetOrAddQueueInfo(schema, cancellationToken);
                var head = stateQueueInfo.HeadKey;
                head++;
                stateQueueInfo.HeadKey = head;
                var id = this._managerInternals.GetKey(new QueueItemStateKey(schema, head));
                var document = this._managerInternals.BuildWrapperGeneric(this._managerInternals.GetOrCreateMetadata(metadata, StateWrapperType.ReliableQueueInfo), id, value);

                await this.SetValueInternalAsync(id, document, typeof(T), cancellationToken);
                await this.SetQueueInfo(schema, stateQueueInfo, cancellationToken);
            }

            public async Task<ConditionalValue<T>> DequeueAsync<T>(string schema,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                this.CheckIsNotReadOnly();

                var stateQueueInfo = await this.GetOrAddQueueInfo(schema, cancellationToken);
                var tail = stateQueueInfo.TailKey;
                var head = stateQueueInfo.HeadKey;

                if ((tail - head) == 1)
                {
                    return new ConditionalValue<T>(false, default(T));
                }

                var id = this._managerInternals.GetKey(new QueueItemStateKey(schema, tail));
                var value = await this.TryGetValueInternalAsync<T>(id, cancellationToken);
                if (!value.HasValue)
                {
                    return new ConditionalValue<T>(false, default(T));
                }

                tail++;

                stateQueueInfo.TailKey = tail;
                await this.SetQueueInfo(schema, stateQueueInfo, cancellationToken);

                await this.RemoveInternalAsync(id, cancellationToken);

                return new ConditionalValue<T>(true, value.Value.State);
            }

            public async Task<ConditionalValue<T>> PeekAsync<T>(string schema,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

                var stateQueueInfo = await this.GetOrAddQueueInfo(schema, cancellationToken);
                var tail = stateQueueInfo.TailKey;
                var head = stateQueueInfo.HeadKey;

                if ((tail - head) == 1)
                {
                    return new ConditionalValue<T>(false, default(T));
                }

                var id = this._managerInternals.GetKey(new QueueItemStateKey(schema, tail));
                var value = await this.TryGetValueInternalAsync<T>(id, cancellationToken);
                if (!value.HasValue)
                {
                    return new ConditionalValue<T>(false, default(T));
                }

                return new ConditionalValue<T>(value.HasValue, value.Value.State);
            }

            public Task<long> GetDictionaryCountAsync<T>(string schema, CancellationToken cancellationToken)
            {
                return this.GetCountInternalAsync(schema, cancellationToken);
            }

            public async Task<long> GetEnqueuedCountAsync<T>(string schema, CancellationToken cancellationToken)
            {
                var stateQueueInfo = await this.GetOrAddQueueInfo(schema, cancellationToken);
                var tail = stateQueueInfo.TailKey;
                var head = stateQueueInfo.HeadKey;

                return head - tail + 1;
            }

            public void Dispose()
            {
                this.DetachObjects();
                this.CommitAsync();
                this._rwLock.Dispose();
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            public Task CommitAsync()
            {
                var changes = this._transactedChanges.Values.ToArray();
                this._transactedChanges.Clear();
                return this.CommitinternalAsync(changes);
            }

            public Task AbortAsync()
            {
                this._transactedChanges.Clear();
                return Task.FromResult(true);
            }

            private void AttachObjects(IEnumerable<IStateSessionReadOnlyObject> stateSessionObjects)
            {
                this._attachedObjects = stateSessionObjects;
                var stateSession = this as TStateSession;
                foreach (var stateSessionObject in this._attachedObjects)
                {
                    if (!(stateSessionObject is StateSessionBaseObject<TStateSession> stateSessionBaseObject))
                    {
                        throw new StateSessionException(
                            $"Can only attach object that have been created by the owning StateSessionManager");
                    }

                    stateSessionBaseObject.AttachToSession(stateSession);
                }
            }

            private void DetachObjects()
            {
                var stateSession = this as TStateSession;
                foreach (var stateSessionObject in this._attachedObjects)
                {
                    if (!(stateSessionObject is StateSessionBaseObject<TStateSession> stateSessionBaseObject))
                    {
                        throw new StateSessionException(
                            $"Can only detach object that have been created by the owning StateSessionManager");
                    }

                    stateSessionBaseObject.DetachFromSession(stateSession);
                }

                this._attachedObjects = new IStateSessionObject[0];
            }

            protected abstract Task<bool> ContainsInternal(SchemaStateKey id,
                CancellationToken cancellationToken = default(CancellationToken));

            protected abstract Task<FindByKeyPrefixResult> FindByKeyPrefixInternalAsync(string schemaKeyPrefix,
                int maxNumResults = 100000,
                ContinuationToken continuationToken = null,
                CancellationToken cancellationToken = new CancellationToken());

            protected abstract Task<IEnumerable<string>> EnumerateSchemaNamesInternalAsync(string schemaKeyPrefix, string key,
                CancellationToken cancellationToken = default(CancellationToken));

            protected abstract Task<ConditionalValue<StateWrapper<T>>> TryGetValueInternalAsync<T>(SchemaStateKey id,
                CancellationToken cancellationToken = default(CancellationToken));

            protected abstract Task<StateWrapper<T>> GetValueInternalAsync<T>(string id,
                CancellationToken cancellationToken = default(CancellationToken));

            protected abstract Task SetValueInternalAsync(SchemaStateKey id, StateWrapper value,
                Type valueType,
                CancellationToken cancellationToken = default(CancellationToken));

            protected abstract Task RemoveInternalAsync(SchemaStateKey id,
                CancellationToken cancellationToken = default(CancellationToken));

            private async Task<QueueInfo> GetOrAddQueueInfo(string schema,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                if (!this._managerInternals.OpenQueues.ContainsKey(schema))
                {
                    throw new StateSessionException($"Queue {schema} must be open before starting a session that uses it");
                }

                QueueInfo queueInfo;
                lock (this._lock)
                {
                    queueInfo = this._managerInternals.OpenQueues[schema];
                    if (queueInfo != null)
                    {
                        return queueInfo;
                    }
                }

                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

                var id = this._managerInternals.GetKey(new QueueInfoStateKey(schema));

                var value = await this.TryGetValueInternalAsync<QueueInfo>(id, cancellationToken);
                if (value.HasValue)
                {
                    lock (this._lock)
                    {
                        queueInfo = value.Value.State;
                        if (queueInfo != null)
                        {
                            this._managerInternals.OpenQueues[schema] = queueInfo;
                            return queueInfo;
                        }
                    }
                }

                queueInfo = new QueueInfo()
                {
                    HeadKey = -1L,
                    TailKey = 0L,
                };
                var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
                var document = this._managerInternals.BuildWrapperGeneric(metadata, id, queueInfo);

                await this.SetValueInternalAsync(id, document, typeof(QueueInfo), cancellationToken);

                lock (this._lock)
                {
                    this._managerInternals.OpenQueues[schema] = queueInfo;
                    return queueInfo;
                }
            }


            private async Task SetQueueInfo(string schema, QueueInfo value,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

                var id = this._managerInternals.GetKey(new QueueInfoStateKey(schema));

                var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
                var document = this._managerInternals.BuildWrapperGeneric(metadata, id, value);

                await this.SetValueInternalAsync(id, document, typeof(QueueInfo), cancellationToken);

                lock (this._lock)
                {
                    this._managerInternals.OpenQueues[schema] = value;
                }
            }

            protected abstract Task<long> GetCountInternalAsync(string schema, CancellationToken cancellationToken);

            protected abstract void Dispose(bool disposing);

            protected virtual async Task CommitinternalAsync(IEnumerable<StateChange> stateChanges)
            {
                foreach (var stateChange in stateChanges)
                {
                    switch (stateChange.ChangeType)
                    {
                        case StateChangeType.None:
                            break;
                        case StateChangeType.AddOrUpdate:
                            await this.SetValueInternalAsync(SchemaStateKey.Parse(stateChange.Value.Id),
                                stateChange.Value, stateChange.ValueType, CancellationToken.None);
                            break;
                        case StateChangeType.Remove:
                            await this.RemoveInternalAsync(SchemaStateKey.Parse(stateChange.Value.Id),
                                CancellationToken.None);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}