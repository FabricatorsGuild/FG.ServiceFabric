using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	internal class StateSessionBaseObject<TStateSession> : IStateSessionObject 
		where TStateSession : class, IStateSession
	{
		protected readonly IStateSessionManagerInternals _manager;
		protected readonly string _schema;
		protected TStateSession _session;

		protected StateSessionBaseObject(IStateSessionManagerInternals manager, string schema)
		{
			_manager = manager;
			_schema = schema;
		}

		internal void AttachToSession(TStateSession session)
		{
			if (_session != null && _session.Equals(session))
			{
				throw new StateSessionException($"Cannot attach StateSessionBaseDictionary to session {session.GetHashCode()}, it is already attached to session {_session.GetHashCode()}");
			}
			_session = session;
		}

		internal void DetachFromSession(TStateSession session)
		{
			if (_session == null || !_session.Equals(session))
			{
				throw new StateSessionException($"Cannot detach StateSessionBaseDictionary from session {session.GetHashCode()}, it is not attached to this session {_session?.GetHashCode()}");
			}
			_session = null;
		}

		protected void CheckSession()
		{
			if (_session == null)
			{
				throw new StateSessionException($"Cannot call methods on a StateSessionDictionary without a StateSession, call StateSessionManager.CreateSession() with this dictionary as an argument");
			}
		}
	}

	internal class StateSessionBaseQueue<TStateSession, TValueType> : StateSessionBaseObject<TStateSession>, IStateSessionQueue<TValueType>, IAsyncEnumerable<TValueType>
		where TStateSession : class, IStateSession
	{

		public StateSessionBaseQueue(IStateSessionManagerInternals manager, string schema)
			: base(manager, schema)
		{
		}

		public Task EnqueueAsync(TValueType value, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _session.EnqueueAsync(_schema, value, null, cancellationToken);
		}
		public Task EnqueueAsync(TValueType value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _session.EnqueueAsync(_schema, value, metadata, cancellationToken);
		}
		public Task<ConditionalValue<TValueType>> DequeueAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return _session.DequeueAsync<TValueType>(_schema, cancellationToken);
		}
		public Task<ConditionalValue<TValueType>> PeekAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return _session.PeekAsync<TValueType>(_schema, cancellationToken);
		}
		public Task<IAsyncEnumerable<TValueType>> CreateEnumerableAsync()
		{
			return Task.FromResult((IAsyncEnumerable<TValueType>) this);
		}
		public Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return _session.GetEnqueuedCountAsync<TValueType>(_schema, cancellationToken);
		}

		IAsyncEnumerator<TValueType> IAsyncEnumerable<TValueType>.GetAsyncEnumerator()
		{
			return new StateSessionBaseQueueEnumerator(_manager, _session, _schema);
		}


		private class StateSessionBaseQueueEnumerator : IAsyncEnumerator<TValueType>
		{
			private readonly IStateSessionManagerInternals _manager;
			private TStateSession _session;
			private readonly string _schema;

			public StateSessionBaseQueueEnumerator(IStateSessionManagerInternals manager, TStateSession session, string schema)
			{
				_manager = manager;
				_session = session;
				_schema = schema;
			}

			public void Dispose()
			{
				Current = default(TValueType);
				_session = null;
			}
			public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
			{
				var schemaKeyPrefix = _manager.GetSchemaQueueStateKeyPrefix(_schema);
				var findNext = await _session.FindByKeyPrefixAsync<TValueType>(_schema, schemaKeyPrefix, 1, CurrentKey != null ? new ContinuationToken(CurrentKey) : null, cancellationToken);
				var currentKey = findNext.Items.FirstOrDefault();
				if(currentKey != null)
				{
					CurrentKey = currentKey;
					Current = await _session.GetValueAsync<TValueType>(_schema, currentKey, cancellationToken);
					return true;
				}
				else
				{
					CurrentKey = null;
					Current = default(TValueType);
					return false;
				}
			}
			private string CurrentKey { get; set; }
			public void Reset() { Current = default(TValueType); }
			public TValueType Current { get; private set; }
		}
	}

	internal class StateSessionBaseDictionary<TStateSessionManager, TStateSession, TValueType> : StateSessionBaseObject<TStateSession>, IStateSessionDictionary<TValueType>, IAsyncEnumerable<KeyValuePair<string, TValueType>>
		where TStateSession : class, IStateSession
	{

		public StateSessionBaseDictionary(IStateSessionManagerInternals manager, string schema)
			: base(manager, schema)
		{
		}

		public Task<bool> Contains(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.Contains<TValueType>(_schema, key, cancellationToken);
		}

		public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string keyPrefix, int maxNumResults = 100000, ContinuationToken continuationToken = null,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.FindByKeyPrefixAsync<TValueType>(_schema, keyPrefix, maxNumResults, continuationToken, cancellationToken);
		}

		public Task<ConditionalValue<TValueType>> TryGetValueAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.TryGetValueAsync<TValueType>(_schema, key, cancellationToken);
		}

		public Task<TValueType> GetValueAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.GetValueAsync<TValueType>(_schema, key, cancellationToken);
		}

		public Task SetValueAsync(string key, TValueType value, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.SetValueAsync(_schema, key, value, null, cancellationToken);
		}

		public Task SetValueAsync(string key, TValueType value, IValueMetadata metadata, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.SetValueAsync(_schema, key, value, metadata, cancellationToken);
		}

		public Task RemoveAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.RemoveAsync<TValueType>(_schema, key, cancellationToken);
		}
		public Task<IAsyncEnumerable<KeyValuePair<string, TValueType>>> CreateEnumerableAsync()
		{
			return Task.FromResult((IAsyncEnumerable<KeyValuePair<string, TValueType>>)this);
		}
		public Task<long> GetCountAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckSession();
			return _session.GetDictionaryCountAsync<TValueType>(_schema, cancellationToken);
		}

		IAsyncEnumerator<KeyValuePair<string, TValueType>> IAsyncEnumerable<KeyValuePair<string, TValueType>>.GetAsyncEnumerator()
		{
			return new StateSessionBaseDictionaryEnumerator(_session, _schema);
		}

		private class StateSessionBaseDictionaryEnumerator : IAsyncEnumerator<KeyValuePair<string, TValueType>>
		{
			private IStateSession _session;
			private readonly string _schema;

			public StateSessionBaseDictionaryEnumerator(IStateSession session, string schema)
			{
				_session = session;
				_schema = schema;
			}

			public void Dispose()
			{
				Current = default(KeyValuePair<string, TValueType>);
				_session = null;
			}
			public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
			{
				var findNext = await _session.FindByKeyPrefixAsync<TValueType>(_schema, null, 1, new ContinuationToken(Current), cancellationToken);
				var currentKey = findNext.Items.FirstOrDefault();
				if (currentKey != null)
				{
					var value = await _session.GetValueAsync<TValueType>(_schema, currentKey, cancellationToken);
					Current = new KeyValuePair<string, TValueType>(currentKey, value);
					return true;
				}
				else
				{
					Current = default(KeyValuePair<string, TValueType>);
					return false;
				}
			}
			public void Reset() { Current = default(KeyValuePair<string, TValueType>); }
			public KeyValuePair<string, TValueType> Current { get; private set; }
		}
	}

	public interface IStateSessionManagerInternals : IStateSessionManager
	{

		string GetSchemaKey();
		string GetSchemaKey(string schema);
		string GetSchemaKey(string schema, string key);

		string GetSchemaFromSchemaKey(string schemaKey);

		string GetSchemaStateKey(string schema, string stateName);

		IServiceMetadata GetMetadata();

		IValueMetadata GetOrCreateMetadata(IValueMetadata metadata, StateWrapperType type);

		StateWrapper BuildWrapper(IValueMetadata valueMetadata, string id, string schema, string key);

		StateWrapper BuildWrapper(IValueMetadata metadata, string id, string schema, string key, Type valueType, object value);

		StateWrapper<T> BuildWrapperGeneric<T>(IValueMetadata valueMetadata, string id, string schema, string key, T value);

		string GetSchemaStateQueueInfoKey(string schema);

		string GetSchemaQueueStateKey(string schema, long index);

		string GetSchemaQueueStateKeyPrefix(string schema);

		IDictionary<string, QueueInfo> OpenQueues { get; }

		string GetEscapedKey(string key);

		string GetUnescapedKey(string key);
	}
	
	public abstract class StateSessionManagerBase<TStateSession> : IStateSessionManager, IStateSessionManagerInternals
		where TStateSession : class, IStateSession
	{
		private readonly IDictionary<string, QueueInfo> _openQueues = new ConcurrentDictionary<string, QueueInfo>();

		IDictionary<string, QueueInfo> IStateSessionManagerInternals.OpenQueues => _openQueues;

		protected Guid PartitionId { get; }
		protected string PartitionKey { get; }
		protected string ServiceName { get; }

		protected StateSessionManagerBase(
			string serviceName,
			Guid partitionId,
			string partitionKey)
		{
			ServiceName = serviceName;
			PartitionId = partitionId;
			PartitionKey = partitionKey;
		}

		protected IStateSessionManagerInternals _managerInternals => (IStateSessionManagerInternals)this;

		string IStateSessionManagerInternals.GetSchemaKey()
		{
			return new StateSessionHelper.SchemaStateKey(ServiceName, PartitionKey, null, null).ToString();
		}
		string IStateSessionManagerInternals.GetSchemaKey(string schema)
		{
			return new StateSessionHelper.SchemaStateKey(ServiceName, PartitionKey, schema, null).ToString();
		}
		string IStateSessionManagerInternals.GetSchemaKey(string schema, string key)
		{
			return new StateSessionHelper.SchemaStateKey(ServiceName, PartitionKey, schema, key).ToString();
		}

		string IStateSessionManagerInternals.GetSchemaFromSchemaKey(string schemaKey)
		{
			return StateSessionHelper.SchemaStateKey.Parse(schemaKey).Schema;
		}

		string IStateSessionManagerInternals.GetSchemaStateKey(string schema, string stateName)
		{
			return StateSessionHelper.GetSchemaStateKey(ServiceName, PartitionKey, schema, stateName);
		}

		IServiceMetadata IStateSessionManagerInternals.GetMetadata()
		{
			return new ServiceMetadata() {ServiceName = ServiceName, PartitionKey = PartitionKey};
		}

		IValueMetadata IStateSessionManagerInternals.GetOrCreateMetadata(IValueMetadata metadata, StateWrapperType type)
		{
			if (metadata == null)
			{
				metadata = new ValueMetadata(type);
			}
			else
			{
				metadata.SetType(type);
			}
			return metadata;
		}

		StateWrapper IStateSessionManagerInternals.BuildWrapper(IValueMetadata valueMetadata, string id, string schema, string key)
		{
			var serviceMetadata = _managerInternals.GetMetadata();
			if (valueMetadata == null) valueMetadata = new ValueMetadata(StateWrapperType.Unknown);
			valueMetadata.Key = key;
			valueMetadata.Schema = schema;
			var wrapper = valueMetadata.BuildStateWrapper(id, serviceMetadata);
			return wrapper;
		}

		StateWrapper IStateSessionManagerInternals.BuildWrapper(IValueMetadata metadata, string id, string schema, string key, Type valueType, object value)
		{
			return (StateWrapper) this.CallGenericMethod(nameof(IStateSessionManagerInternals.BuildWrapperGeneric), new Type[] {valueType},
			                                             _managerInternals.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id, schema, key, value);
		}

		StateWrapper<T> IStateSessionManagerInternals.BuildWrapperGeneric<T>(IValueMetadata valueMetadata, string id, string schema, string key, T value)
		{
			var serviceMetadata = _managerInternals.GetMetadata();
			if (valueMetadata == null) valueMetadata = new ValueMetadata(StateWrapperType.Unknown);
			valueMetadata.Key = key;
			valueMetadata.Schema = schema;
			var wrapper = valueMetadata.BuildStateWrapper(id, value, serviceMetadata);
			return wrapper;
		}		

		string IStateSessionManagerInternals.GetSchemaStateQueueInfoKey(string schema)
		{
			return StateSessionHelper.GetSchemaStateQueueInfoKey(ServiceName, PartitionKey, schema);
		}

		string IStateSessionManagerInternals.GetSchemaQueueStateKey(string schema, long index)
		{
			return StateSessionHelper.GetSchemaQueueStateKey(ServiceName, PartitionKey, schema, index);
		}

		string IStateSessionManagerInternals.GetSchemaQueueStateKeyPrefix(string schema)
		{
			return StateSessionHelper.GetSchemaStateKeyPrefix(ServiceName, PartitionKey, schema);
		}

		string IStateSessionManagerInternals.GetEscapedKey(string key) { return GetEscapedKeyInternal(key); }
		string IStateSessionManagerInternals.GetUnescapedKey(string key) { return GetUnescapedKeyInternal(key); } 

		protected virtual string GetEscapedKeyInternal(string key) { return key; }
		protected virtual string GetUnescapedKeyInternal(string key) { return key; }


		public Task<IStateSessionDictionary<T>> OpenDictionary<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			var dictionary = (IStateSessionDictionary<T>)new StateSessionBaseDictionary<StateSessionManagerBase<TStateSession>, TStateSession, T>(this, schema);
			return Task.FromResult(dictionary);			
		}

		public Task<IStateSessionQueue<T>> OpenQueue<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			if (!_openQueues.ContainsKey(schema))
			{
				_openQueues.Add(schema, null);
			}
			var queue = (IStateSessionQueue<T>)new StateSessionBaseQueue<TStateSession, T>(this, schema);
			return Task.FromResult(queue);
		}

		public IStateSession CreateSession(params IStateSessionObject[] stateSessionObjects)
		{
			var session = CreateSessionInternal(this, stateSessionObjects);
			return session;
		}

		protected abstract TStateSession CreateSessionInternal(StateSessionManagerBase<TStateSession> manager, IStateSessionObject[] stateSessionObjects);

		public enum StateChangeType
		{
			None = 0,
			AddOrUpdate = 1,
			Remove = 2,
		}

		public class StateChange
		{

			public StateChange(StateChangeType changeType, StateWrapper value, Type valueType)
			{
				ChangeType = changeType;
				Value = value;
				ValueType = valueType;
			}
			public StateChangeType ChangeType { get; set; }
			public StateWrapper Value { get; set; }
			public Type ValueType { get; set; }
		}

		public abstract class StateSessionBase<TStateSessionManager> : IStateSession
			where TStateSessionManager : class, IStateSessionManagerInternals, IStateSessionManager
		{
			private readonly TStateSessionManager _manager;
			private IStateSessionManagerInternals _managerInternals => _manager;
			private readonly object _lock = new object();
			private readonly IDictionary<string, StateChange> _transactedChanges = new ConcurrentDictionary<string, StateChange>();
			private IEnumerable<IStateSessionObject> _attachedObjects;

			protected StateSessionBase(TStateSessionManager manager, IEnumerable<IStateSessionObject> stateSessionObjects)
			{
				AttachObjects(stateSessionObjects);
				_manager = manager;
			}

			private void AttachObjects(IEnumerable<IStateSessionObject> stateSessionObjects)
			{
				_attachedObjects = stateSessionObjects;
				var stateSession = this as TStateSession;
				foreach (var stateSessionObject in _attachedObjects)
				{
					if (!(stateSessionObject is StateSessionBaseObject<TStateSession> stateSessionBaseObject))
					{
						throw new StateSessionException($"Can only attach object that have been created by the owning StateSessionManager");
					}
					stateSessionBaseObject.AttachToSession(stateSession);
				}
			}

			private void DetachObjects()
			{
				var stateSession = this as TStateSession;
				foreach (var stateSessionObject in _attachedObjects)
				{
					if (!(stateSessionObject is StateSessionBaseObject<TStateSession> stateSessionBaseObject))
					{
						throw new StateSessionException($"Can only detach object that have been created by the owning StateSessionManager");
					}
					stateSessionBaseObject.DetachFromSession(stateSession);
				}
				_attachedObjects = new IStateSessionObject[0];
			}

			public Task<bool> Contains<T>(string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				return Contains(schema, key, cancellationToken);
			}

			public Task<bool> Contains(string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _managerInternals.GetSchemaStateKey(schema, key);

				if (_transactedChanges.ContainsKey(id))
				{
					// Check if session contains it, or if it has been removed from session
					switch (_transactedChanges[id].ChangeType)
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
				return ContainsInternal(id, schema, key, cancellationToken);
			}

			protected abstract Task<bool> ContainsInternal(string id, string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken));
			
			public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync<T>(string schema, string keyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				return FindByKeyPrefixAsync(schema, keyPrefix, maxNumResults, continuationToken, cancellationToken);
			}

			public async Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string schema, string keyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var schemaPrefix = _managerInternals.GetSchemaKey(schema);
				var schemaKeyPrefix = _managerInternals.GetSchemaKey(schema, _manager.GetEscapedKey(keyPrefix));
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
				
				var result = await FindByKeyPrefixInternalAsync(schemaKeyPrefix, maxNumResults, continuationToken, cancellationToken);
				return result;
			}

			protected abstract Task<FindByKeyPrefixResult> FindByKeyPrefixInternalAsync(string schemaKeyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken());

			public async Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var schemaKeyPrefix = _managerInternals.GetSchemaKey();
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
				var result = await EnumerateSchemaNamesInternalAsync(schemaKeyPrefix, key, cancellationToken);
				return result;
			}

			protected abstract Task<IEnumerable<string>> EnumerateSchemaNamesInternalAsync(string schemaKeyPrefix, string key,
				CancellationToken cancellationToken = default(CancellationToken));

			public async Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _managerInternals.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));

				// Check if session contains it, or if it has been removed from session
				if (_transactedChanges.ContainsKey(id))
				{
					var transactedChange = _transactedChanges[id];
					switch (transactedChange.ChangeType)
					{
						case StateChangeType.AddOrUpdate:
							return new ConditionalValue<T>(true, ((StateWrapper<T>) transactedChange.Value).State);
						case StateChangeType.Remove:
							return new ConditionalValue<T>(false, default(T));
						case StateChangeType.None:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
				var value = await TryGetValueInternalAsync<T>(id, schema, key, cancellationToken);
				if (value.HasValue)
				{
					return new ConditionalValue<T>(true, value.Value.State);
				}
				return new ConditionalValue<T>(false, default(T));
			}

			protected abstract Task<ConditionalValue<StateWrapper<T>>> TryGetValueInternalAsync<T>(string id, string schema, string key,
				CancellationToken cancellationToken = default(CancellationToken));

			public async Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _managerInternals.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));

				// Check if session contains it, or if it has been removed from session
				if (_transactedChanges.ContainsKey(id))
				{
					var transactedChange = _transactedChanges[id];
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

				// Check if session contains it, or if it has been removed from session
				if (_transactedChanges.ContainsKey(id))
				{
					var transactedChange = _transactedChanges[id];
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
				var value = await GetValueInternalAsync<T>(id, cancellationToken);
				if (value != null)
				{
					return value.State;
				}
				throw new StateSessionException($"Failed to GetValueAsync for {schema}:{key}");
			}

			protected abstract Task<StateWrapper<T>> GetValueInternalAsync<T>(string id, 
				CancellationToken cancellationToken = default(CancellationToken));

			public Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _managerInternals.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				var valueType = typeof(T);
				var document = _managerInternals.BuildWrapperGeneric<T>(_managerInternals.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id, schema, key, value);

				_transactedChanges[id] = new StateChange(StateChangeType.AddOrUpdate, document, valueType);
				return Task.FromResult(true);
			}

			public Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metadata,
				CancellationToken cancellationToken = new CancellationToken())
			{
				var id = _managerInternals.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				var document = _managerInternals.BuildWrapper(_managerInternals.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id, schema, key, valueType, value);

				_transactedChanges[id] = new StateChange(StateChangeType.AddOrUpdate, document, valueType);
				return Task.FromResult(true);
			}

			protected abstract Task SetValueInternalAsync(string id, string schema, string key, StateWrapper value, Type valueType,
				CancellationToken cancellationToken = default(CancellationToken));

			public Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				return RemoveAsync(schema, key, cancellationToken);
			}

			public Task RemoveAsync(string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _managerInternals.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var stateWrapper = _managerInternals.BuildWrapper(_managerInternals.GetOrCreateMetadata(null, StateWrapperType.ReliableDictionaryItem), id, schema, key);

				_transactedChanges[id] = new StateChange(StateChangeType.Remove, stateWrapper, null);
				return Task.FromResult(true);
			}

			protected abstract Task RemoveInternalAsync(string id, string schema, string key,
				CancellationToken cancellationToken = default(CancellationToken));

			private async Task<QueueInfo> GetOrAddQueueInfo(string schema,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				if (!_managerInternals.OpenQueues.ContainsKey(schema))
				{
					throw new StateSessionException($"Queue {schema} must be open before starting a session that uses it");
				}

				QueueInfo queueInfo;
				lock (_lock)
				{
					queueInfo = _managerInternals.OpenQueues[schema];
					if (queueInfo != null)
					{
						return queueInfo;
					}
				}

				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var id = _managerInternals.GetSchemaStateQueueInfoKey(schema);
				var key = StateSessionHelper.ReliableStateQueueInfoName;

				var value = await TryGetValueInternalAsync<QueueInfo>(id, schema, key, cancellationToken);
				if (value.HasValue)
				{
					lock (_lock)
					{
						queueInfo = value.Value.State;
						if (queueInfo != null)
						{
							_managerInternals.OpenQueues[schema] = queueInfo;
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
				var document = _managerInternals.BuildWrapperGeneric(metadata, id, schema, key, queueInfo);

				await SetValueInternalAsync(id, schema, key, document, typeof(QueueInfo), cancellationToken);

				lock (_lock)
				{
					_managerInternals.OpenQueues[schema] = queueInfo;
					return queueInfo;
				}
			}
			
			

			private async Task SetQueueInfo(string schema, QueueInfo value,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var id = _managerInternals.GetSchemaStateQueueInfoKey(schema);
				var key = StateSessionHelper.ReliableStateQueueInfoName;
				var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
				var document = _managerInternals.BuildWrapperGeneric(metadata, id, schema, key, value);

				await SetValueInternalAsync(id, schema, key, document, typeof(QueueInfo), cancellationToken);

				lock (_lock)
				{
					_managerInternals.OpenQueues[schema] = value;
				}
			}

			public async Task EnqueueAsync<T>(string schema, T value, IValueMetadata metadata, CancellationToken cancellationToken = new CancellationToken())
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var stateQueueInfo = await GetOrAddQueueInfo(schema, cancellationToken);
				var head = stateQueueInfo.HeadKey;
				head++;
				stateQueueInfo.HeadKey = head;
				var key = head.ToString();
				var id = _managerInternals.GetSchemaStateKey(schema, key);
				var document = _managerInternals.BuildWrapperGeneric(_managerInternals.GetOrCreateMetadata(metadata, StateWrapperType.ReliableQueueInfo), id, schema, key, value);

				await SetValueInternalAsync(id, schema, key, document, typeof(T), cancellationToken);
				await SetQueueInfo(schema, stateQueueInfo, cancellationToken);
			}

			public async Task<ConditionalValue<T>> DequeueAsync<T>(string schema,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var stateQueueInfo = await GetOrAddQueueInfo(schema, cancellationToken);
				var tail = stateQueueInfo.TailKey;
				var head = stateQueueInfo.HeadKey;

				if ((tail - head) == 1)
				{
					return new ConditionalValue<T>(false, default(T));
				}
				var id = _managerInternals.GetSchemaQueueStateKey(schema, tail);
				var key = tail.ToString();
				var value = await TryGetValueInternalAsync<T>(id, schema, key, cancellationToken);
				if (!value.HasValue)
				{
					return new ConditionalValue<T>(false, default(T));
				}

				tail++;

				stateQueueInfo.TailKey = tail;
				await SetQueueInfo(schema, stateQueueInfo, cancellationToken);

				await RemoveInternalAsync(id, schema, key, cancellationToken);

				return new ConditionalValue<T>(true, value.Value.State);
			}
			
			public async Task<ConditionalValue<T>> PeekAsync<T>(string schema,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var stateQueueInfo = await GetOrAddQueueInfo(schema, cancellationToken);
				var tail = stateQueueInfo.TailKey;
				var head = stateQueueInfo.HeadKey;

				if ((tail - head) == 1)
				{
					return new ConditionalValue<T>(false, default(T));
				}
				var id = _managerInternals.GetSchemaQueueStateKey(schema, tail);
				var key = tail.ToString();
				var value = await TryGetValueInternalAsync<T>(id, schema, key, cancellationToken);
				if (!value.HasValue)
				{
					return new ConditionalValue<T>(false, default(T));
				}
				return new ConditionalValue<T>(value.HasValue, value.Value.State);
			}
			public Task<long> GetDictionaryCountAsync<T>(string schema, CancellationToken cancellationToken)
			{
				return GetCountInternalAsync(schema, cancellationToken);
			}

			protected abstract Task<long> GetCountInternalAsync(string schema, CancellationToken cancellationToken);

			public async Task<long> GetEnqueuedCountAsync<T>(string schema, CancellationToken cancellationToken)
			{
				var stateQueueInfo = await GetOrAddQueueInfo(schema, cancellationToken);
				var tail = stateQueueInfo.TailKey;
				var head = stateQueueInfo.HeadKey;

				return (head - tail);
			}

			protected abstract void Dispose(bool disposing);

			public void Dispose()
			{
				DetachObjects();
				CommitAsync();
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			public Task CommitAsync()
			{
				return CommitinternalAsync(_transactedChanges.Values);
			}

			protected virtual async Task CommitinternalAsync(IEnumerable<StateChange> stateChanges)
			{
				foreach (var stateChange in stateChanges)
				{
					switch (stateChange.ChangeType)
					{
						case StateChangeType.None:
							break;
						case StateChangeType.AddOrUpdate:
							await SetValueInternalAsync(stateChange.Value.Id, stateChange.Value.Schema, stateChange.Value.Key, stateChange.Value, stateChange.ValueType, CancellationToken.None);
							break;
						case StateChangeType.Remove:
							await RemoveInternalAsync(stateChange.Value.Id, stateChange.Value.Schema, stateChange.Value.Key, CancellationToken.None);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			public Task AbortAsync()
			{
				_transactedChanges.Clear();
				return Task.FromResult(true);
			}
		}
	}
}