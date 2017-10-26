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
	public abstract class StateSessionManagerBase<TStateSession> : IStateSessionManager
		where TStateSession : IStateSession
	{
		private readonly IDictionary<string, QueueInfo> _openQueues = new ConcurrentDictionary<string, QueueInfo>();

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

		protected string GetSchemaKey(string schema = null, string key = null)
		{
			return new StateSessionHelper.SchemaStateKey(ServiceName, PartitionKey, schema, key).ToString();
		}

		protected string GetSchemaFromSchemaKey(string schemaKey)
		{
			return StateSessionHelper.SchemaStateKey.Parse(schemaKey).Schema;
		}

		protected string GetSchemaStateKey(string schema, string stateName)
		{
			return StateSessionHelper.GetSchemaStateKey(ServiceName, PartitionKey, schema, stateName);
		}

		private IServiceMetadata GetMetadata()
		{
			return new ServiceMetadata() {ServiceName = ServiceName, PartitionKey = PartitionKey};
		}

		protected IValueMetadata GetOrCreateMetadata(IValueMetadata metadata, StateWrapperType type)
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

		protected StateWrapper BuildWrapper(IValueMetadata valueMetadata, string id, string schema, string key)
		{
			var serviceMetadata = GetMetadata();
			if (valueMetadata == null) valueMetadata = new ValueMetadata(StateWrapperType.Unknown);
			valueMetadata.Key = key;
			valueMetadata.Schema = schema;
			var wrapper = valueMetadata.BuildStateWrapper(id, serviceMetadata);
			return wrapper;
		}

		protected StateWrapper BuildWrapper(IValueMetadata metadata, string id, string schema, string key, Type valueType, object value)
		{
			return (StateWrapper) this.CallGenericMethod(nameof(BuildWrapperGeneric), new Type[] {valueType},
				this.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id, schema, key, value);
		}

		protected StateWrapper<T> BuildWrapperGeneric<T>(IValueMetadata valueMetadata, string id, string schema, string key, T value)
		{
			var serviceMetadata = GetMetadata();
			if (valueMetadata == null) valueMetadata = new ValueMetadata(StateWrapperType.Unknown);
			valueMetadata.Key = key;
			valueMetadata.Schema = schema;
			var wrapper = valueMetadata.BuildStateWrapper(id, value, serviceMetadata);
			return wrapper;
		}		

		protected string GetSchemaStateQueueInfoKey(string schema)
		{
			return StateSessionHelper.GetSchemaStateQueueInfoKey(ServiceName, PartitionKey, schema);
		}

		protected string GetSchemaQueueStateKey(string schema, long index)
		{
			return StateSessionHelper.GetSchemaQueueStateKey(ServiceName, PartitionKey, schema, index);
		}

		protected abstract string GetEscapedKey(string key);
		protected abstract string GetUnescapedKey(string key);

		public Task OpenDictionary<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}

		public Task OpenQueue<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			if (!_openQueues.ContainsKey(schema))
			{
				_openQueues.Add(schema, null);
			}
			return Task.FromResult(true);
		}

		public IStateSession CreateSession()
		{
			return CreateSessionInternal(this);
		}

		protected abstract TStateSession CreateSessionInternal(StateSessionManagerBase<TStateSession> manager);

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

		public abstract class StateSessionBase : IStateSession
		{
			private readonly StateSessionManagerBase<TStateSession> _manager;
			private readonly object _lock = new object();
			private readonly IDictionary<string, StateChange> _transactedChanges = new ConcurrentDictionary<string, StateChange>();

			protected StateSessionBase(StateSessionManagerBase<TStateSession> manager)
			{
				_manager = manager;
			}

			public Task<bool> Contains<T>(string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				return Contains(schema, key, cancellationToken);
			}

			public Task<bool> Contains(string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));

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
				var schemaPrefix = _manager.GetSchemaKey(schema);
				var schemaKeyPrefix = _manager.GetSchemaKey(schema, _manager.GetEscapedKey(keyPrefix));
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
				var schemaKeyPrefix = _manager.GetSchemaKey();
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
				var result = await EnumerateSchemaNamesInternalAsync(schemaKeyPrefix, key, cancellationToken);
				return result;
			}

			protected abstract Task<IEnumerable<string>> EnumerateSchemaNamesInternalAsync(string schemaKeyPrefix, string key,
				CancellationToken cancellationToken = default(CancellationToken));

			public async Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));

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
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));

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
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				var valueType = typeof(T);
				var document = _manager.BuildWrapperGeneric<T>(_manager.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id, schema, key, value);

				_transactedChanges[id] = new StateChange(StateChangeType.AddOrUpdate, document, valueType);
				return Task.FromResult(true);
			}

			public Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metadata,
				CancellationToken cancellationToken = new CancellationToken())
			{
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				var document = _manager.BuildWrapper(_manager.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id, schema, key, valueType, value);

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
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var stateWrapper =_manager.BuildWrapper(_manager.GetOrCreateMetadata(null, StateWrapperType.ReliableDictionaryItem), id, schema, key);

				_transactedChanges[id] = new StateChange(StateChangeType.Remove, stateWrapper, null);
				return Task.FromResult(true);
			}

			protected abstract Task RemoveInternalAsync(string id, string schema, string key,
				CancellationToken cancellationToken = default(CancellationToken));

			private async Task<QueueInfo> GetOrAddQueueInfo(string schema,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				if (!_manager._openQueues.ContainsKey(schema))
				{
					throw new StateSessionException($"Queue {schema} must be open before starting a session that uses it");
				}

				QueueInfo queueInfo;
				lock (_lock)
				{
					queueInfo = _manager._openQueues[schema];
					if (queueInfo != null)
					{
						return queueInfo;
					}
				}

				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var id = _manager.GetSchemaStateQueueInfoKey(schema);
				var key = StateSessionHelper.ReliableStateQueueInfoName;

				var value = await TryGetValueInternalAsync<QueueInfo>(id, schema, key, cancellationToken);
				if (value.HasValue)
				{
					lock (_lock)
					{
						queueInfo = value.Value.State;
						if (queueInfo != null)
						{
							_manager._openQueues[schema] = queueInfo;
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
				var document = _manager.BuildWrapperGeneric(metadata, id, schema, key, queueInfo);

				await SetValueInternalAsync(id, schema, key, document, typeof(QueueInfo), cancellationToken);

				lock (_lock)
				{
					_manager._openQueues[schema] = queueInfo;
					return queueInfo;
				}
			}
			
			

			private async Task SetQueueInfo(string schema, QueueInfo value,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var id = _manager.GetSchemaStateQueueInfoKey(schema);
				var key = StateSessionHelper.ReliableStateQueueInfoName;
				var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
				var document = _manager.BuildWrapperGeneric(metadata, id, schema, key, value);

				await SetValueInternalAsync(id, schema, key, document, typeof(QueueInfo), cancellationToken);

				lock (_lock)
				{
					_manager._openQueues[schema] = value;
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
				var id = _manager.GetSchemaStateKey(schema, key);
				var document = _manager.BuildWrapperGeneric(_manager.GetOrCreateMetadata(metadata, StateWrapperType.ReliableQueueInfo), id, schema, key, value);

				await SetValueInternalAsync(id, schema, key, document, typeof(T), cancellationToken);
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
				var id = _manager.GetSchemaQueueStateKey(schema, tail);
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
				var id = _manager.GetSchemaQueueStateKey(schema, tail);
				var key = tail.ToString();
				var value = await TryGetValueInternalAsync<T>(id, schema, key, cancellationToken);
				if (!value.HasValue)
				{
					return new ConditionalValue<T>(false, default(T));
				}
				return new ConditionalValue<T>(value.HasValue, value.Value.State);
			}			

			protected abstract void Dispose(bool disposing);

			public void Dispose()
			{
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