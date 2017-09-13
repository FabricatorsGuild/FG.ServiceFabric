using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.Azure.Documents;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public abstract class StateSessionManagerBase
	{
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

		protected StateWrapper<T> BuildWrapper<T>(IValueMetadata valueMetadata, string id, string schema, string key, T value)
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

		public abstract class StateSessionBase : IStateSession
		{
			private readonly StateSessionManagerBase _manager;
			private object _lock = new object();

			protected StateSessionBase(StateSessionManagerBase manager)
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

				return new FindByKeyPrefixResult()
					{
						ContinuationToken = result.ContinuationToken,
						Items = result.Items.Select(i => _manager.GetUnescapedKey(i.Substring(schemaPrefix.Length))).ToArray()
					};
			}

			protected abstract Task<FindByKeyPrefixResult> FindByKeyPrefixInternalAsync(string schemaKeyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken());

			public async Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var schemaKeyPrefix = _manager.GetSchemaKey();
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
				var result = await EnumerateSchemaNamesInternalAsync(schemaKeyPrefix, cancellationToken);
				return result.Select(file => _manager.GetSchemaFromSchemaKey(file));
			}

			protected abstract Task<IEnumerable<string>> EnumerateSchemaNamesInternalAsync(string schemaKeyPrefix, 
				CancellationToken cancellationToken = default(CancellationToken));

			public Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
				return TryGetValueInternalAsync<T>(id, cancellationToken);
			}

			protected abstract Task<ConditionalValue<T>> TryGetValueInternalAsync<T>(string id, 
				CancellationToken cancellationToken = default(CancellationToken));

			public Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
				return GetValueInternalAsync<T>(id, cancellationToken);
			}

			protected abstract Task<T> GetValueInternalAsync<T>(string id, 
				CancellationToken cancellationToken = default(CancellationToken));

			public Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata, 
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _manager.GetSchemaStateKey(schema, _manager.GetEscapedKey(key));
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
				var document = _manager.BuildWrapper(_manager.GetOrCreateMetadata(metadata, StateWrapperType.ReliableDictionaryItem), id, schema, key, value);
				return SetValueInternalAsync<T>(id, schema, key, document, cancellationToken);
			}

			public Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metaData,
				CancellationToken cancellationToken = new CancellationToken())
			{
				return (Task)this.CallGenericMethod(nameof(SetValueAsync), new Type[] {valueType}, schema, key, value, metaData, cancellationToken);
			}

			protected abstract Task SetValueInternalAsync<T>(string id, string schema, string key, StateWrapper<T> value,
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
				return RemoveInternalAsync(id, schema, key, cancellationToken);
			}

			protected abstract Task RemoveInternalAsync(string id, string schema, string key,
				CancellationToken cancellationToken = default(CancellationToken));

			private async Task<QueueInfo> GetOrAddQueueInfo(string schema,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var id = _manager.GetSchemaStateQueueInfoKey(schema);
				var key = StateSessionHelper.ReliableStateQueueInfoName;
				var queueInfo = await SafeGetQueueInfoInternalAsync(id, schema, key, cancellationToken);
				if (queueInfo != null)
				{
					return queueInfo;
				}

				queueInfo = new QueueInfo()
				{
					HeadKey = -1L,
					TailKey = 0L,
				};
				var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
				var document = _manager.BuildWrapper(metadata, id, schema, key, queueInfo);

				await SetQueueInfoInternalAsync(id, schema, key, document, cancellationToken);

				return queueInfo;
			}

			protected abstract Task<QueueInfo> SafeGetQueueInfoInternalAsync(string id, string schema, string key,
				CancellationToken cancellationToken = default(CancellationToken));

			protected abstract Task<QueueInfo> SetQueueInfoInternalAsync(string id, string schema, string key, StateWrapper<QueueInfo> value,
				CancellationToken cancellationToken = default(CancellationToken));

			private Task<QueueInfo> SetQueueInfo(string schema, QueueInfo value,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				cancellationToken = cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;

				var id = _manager.GetSchemaStateQueueInfoKey(schema);
				var key = StateSessionHelper.ReliableStateQueueInfoName;
				var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
				var document = _manager.BuildWrapper(metadata, id, schema, key, value);
				return SetQueueInfoInternalAsync(id, schema, key, document, cancellationToken);
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
				var document = _manager.BuildWrapper(_manager.GetOrCreateMetadata(metadata, StateWrapperType.ReliableQueueInfo), id, schema, key, value);

				await SetValueInternalAsync(id, schema, key, document, cancellationToken);
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
				var value = await DequeueInternalAsync<T>(id, schema, key, cancellationToken);
				tail++;

				stateQueueInfo.TailKey = tail;
				await SetQueueInfo(schema, stateQueueInfo, cancellationToken);

				return value;
			}

			protected abstract Task<ConditionalValue<T>> DequeueInternalAsync<T>(string id, string schema, string key,
				CancellationToken cancellationToken = new CancellationToken());

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
				var value = await PeekInternalAsync<T>(id, schema, key, cancellationToken);

				return value;

			}

			protected abstract Task<ConditionalValue<T>> PeekInternalAsync<T>(string id, string schema, string key,
				CancellationToken cancellationToken = new CancellationToken());

			public abstract void Dispose();

			public abstract Task CommitAsync();

			public abstract Task AbortAsync();
		}
	}


	public abstract class TextStateSessionManager : StateSessionManagerBase, IStateSessionManager
	{
		protected TextStateSessionManager(
			string serviceName,
			Guid partitionId,
			string partitionKey) :
			base(serviceName, partitionId, partitionKey)
		{
			
		}

		public Task OpenDictionary<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}

		public Task OpenQueue<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}

		protected abstract TextStateSession CreateSessionInner(TextStateSessionManager manager);

		public IStateSession CreateSession()
		{
			return CreateSessionInner(this);
		}

		protected abstract class TextStateSession : IStateSession
		{
			private readonly TextStateSessionManager _manager;
			private readonly object _lock = new object();

			protected TextStateSession(
				TextStateSessionManager manager)
			{
				_manager = manager;
			}

			protected virtual string GetEscapedKey(string id)
			{
				if (string.IsNullOrWhiteSpace(id)) return id;
				return System.Uri.EscapeDataString(id).Replace("%7B", "{").Replace("%7D", "}");
			}

			protected virtual string GetUnescapedKey(string key)
			{
				if (string.IsNullOrWhiteSpace(key)) return key;
				return System.Uri.UnescapeDataString(key.Replace("{", "%7B").Replace("}", "%7D"));
			}

			protected abstract bool Contains(string id);

			protected abstract string Read(string id);

			protected abstract void Delete(string id);

			protected abstract void Write(string id, string content);

			protected abstract FindByKeyPrefixResult Find(string idPrefix, string key, int maxNumResults = 100000, ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken());

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (disposing)
				{

				}
			}

			public Task<bool> Contains<T>(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				return Contains(schema, key, cancellationToken);
			}

			public Task<bool> Contains(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _manager.GetSchemaStateKey(schema, GetEscapedKey(key));
				try
				{
					lock (_lock)
					{
						return Task.FromResult(Contains(id));
					}
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"TryGetValueAsync for {id} failed", ex);
				}
			}			

			public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync<T>(string schema, string keyPrefix, int maxNumResults = 100000, ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken())
			{
				return FindByKeyPrefixAsync(schema, keyPrefix, maxNumResults, continuationToken, cancellationToken);
			}

			public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string schema, string keyPrefix, int maxNumResults = 100000, ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken())
			{
				var schemaPrefix = _manager.GetSchemaKey(schema);
				var schemaKeyPrefix = _manager.GetSchemaKey(schema, GetEscapedKey(keyPrefix));
				var result = Find(schemaKeyPrefix, null, maxNumResults, continuationToken, cancellationToken);

				return
					Task.FromResult(new FindByKeyPrefixResult()
					{
						ContinuationToken = result.ContinuationToken,
						Items = result.Items.Select(i => GetUnescapedKey(i.Substring(schemaPrefix.Length))).ToArray()
					});
			}

			public Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				var schemaKeyPrefix = _manager.GetSchemaKey();
				return Task.FromResult(
					Find(schemaKeyPrefix, key, int.MaxValue, null, cancellationToken)
					.Items.Select(file => _manager.GetSchemaFromSchemaKey(file)));
			}

			public Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				var id = _manager.GetSchemaStateKey(schema, GetEscapedKey(key));
				try
				{
					T value;
					lock (_lock)
					{
						if (!Contains(id))
						{
							return Task.FromResult(new ConditionalValue<T>(false, default(T)));
						}
						var stringValue = Read(id);

						var response = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue);
						value = response.State;
					}
					return Task.FromResult(new ConditionalValue<T>(true, value));
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"TryGetValueAsync for {id} failed", ex);
				}
			}

			public Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				var id = _manager.GetSchemaStateKey(schema, GetEscapedKey(key));
				try
				{
					T value;
					lock (_lock)
					{
						if (!Contains(id))
						{
							throw new KeyNotFoundException($"State with {schema}:{key} does not exist");
						}
						var stringValue = Read(id);

						var response = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue);
						value = response.State;
					}
					return Task.FromResult(value);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"TryGetValueAsync for {id} failed", ex);
				}
			}

			public Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata, CancellationToken cancellationToken = new CancellationToken())
			{
				var id = _manager.GetSchemaStateKey(schema, GetEscapedKey(key));
				try
				{
					lock (_lock)
					{
						var wrapper = _manager.BuildWrapper(metadata, id, schema, key, value);
						var stringValue = JsonConvert.SerializeObject(wrapper, new JsonSerializerSettings() {Formatting = Formatting.Indented});

						if (value == null)
						{
							if (Contains(id))
							{
								Delete(id);
							}
						}
						else
						{
							Write(id, stringValue);
						}
					}

					return Task.FromResult(true);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"SetValueAsync for {id} failed", ex);
				}
			}

			public Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metaData, CancellationToken cancellationToken = new CancellationToken())
			{
				var id = _manager.GetSchemaStateKey(schema, GetEscapedKey(key));
				try
				{
					lock (_lock)
					{

						var wrapper = _manager.CallGenericMethod(nameof(_manager.BuildWrapper), new Type[] {valueType}, metaData, id, schema, key, value);
						var stringValue = JsonConvert.SerializeObject(wrapper, new JsonSerializerSettings() {Formatting = Formatting.Indented});

						if (value == null)
						{
							if (Contains(id))
							{
								Delete(id);
							}
						}
						else
						{
							Write(id, stringValue);
						}
					}

					return Task.FromResult(true);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"SetValueAsync for {id} failed", ex);
				}
			}

			public Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				return RemoveAsync(schema, key, cancellationToken);
			}
			
			public Task RemoveAsync(string schema, string key, CancellationToken cancellationToken = default(CancellationToken))
			{
				var id = _manager.GetSchemaStateKey(schema, GetEscapedKey(key));
				try
				{
					lock (_lock)
					{
						if (Contains(id))
						{
							Delete(id);
						}
					}

					return Task.FromResult(true);
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"RemoveAsync for {id} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"RemoveAsync for {id} failed", ex);
				}
			}

			private Task<QueueInfo> GetOrAddQueueInfo(string schema)
			{
				var id = _manager.GetSchemaStateQueueInfoKey(schema);
				try
				{
					lock (_lock)
					{
						if (Contains(id))
						{
							var stringValue = Read(id);

							var queueInfoResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<QueueInfo>>(stringValue);
							var stateQueueInfo = queueInfoResponse.State;
							return Task.FromResult(stateQueueInfo);
						}

					}

					var value = new QueueInfo()
					{
						HeadKey = -1L,
						TailKey = 0L,
					};

					return SetQueueInfo(schema, value);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"EnqueueAsync for {id} failed", ex);
				}
			}

			private Task<QueueInfo> SetQueueInfo(string schema, QueueInfo value)
			{
				var id = _manager.GetSchemaStateQueueInfoKey(schema);
				var key = StateSessionHelper.ReliableStateQueueInfoName;
				try
				{
					lock (_lock)
					{
						var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
						var document = _manager.BuildWrapper(metadata, id, schema, key, value);
						var stringValue = JsonConvert.SerializeObject(document, new JsonSerializerSettings() {Formatting = Formatting.Indented});
						Write(id, stringValue);
					}
					return Task.FromResult(value);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"CreateQueueInfo for {id} failed", ex);
				}
			}

			public async Task EnqueueAsync<T>(string schema, T value, IValueMetadata metadata, CancellationToken cancellationToken = new CancellationToken())
			{
				var stateKeyQueueInfo = _manager.GetSchemaStateQueueInfoKey(schema);
				var stateQueueInfo = await GetOrAddQueueInfo(schema);
				try
				{
					lock (_lock)
					{
						var head = stateQueueInfo.HeadKey;
						head++;
						stateQueueInfo.HeadKey = head;

						var id = _manager.GetSchemaQueueStateKey(schema, head);

						Console.WriteLine($"Enqueued {value} t:{stateQueueInfo.TailKey} h:{stateQueueInfo.HeadKey}");

						var document = _manager.BuildWrapper(metadata, id, schema, head.ToString(), value);
						var stringValue = JsonConvert.SerializeObject(document, new JsonSerializerSettings() {Formatting = Formatting.Indented});
						Write(id, stringValue);
					}
					await SetQueueInfo(schema, stateQueueInfo);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"EnqueueAsync for {stateKeyQueueInfo} failed", ex);
				}
			}

			public async Task<ConditionalValue<T>> DequeueAsync<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
			{
				var stateKeyQueueInfo = _manager.GetSchemaStateQueueInfoKey(schema);
				var stateQueueInfo = await GetOrAddQueueInfo(schema);
				try
				{
					var tail = stateQueueInfo.TailKey;
					var head = stateQueueInfo.HeadKey;

					if ((tail - head) == 1)
					{
						Console.WriteLine($"Dequeued [null] t:{stateQueueInfo.TailKey} h:{stateQueueInfo.HeadKey}");

						return new ConditionalValue<T>(false, default(T));
					}

					T value;
					lock (_lock)
					{
						var id = _manager.GetSchemaQueueStateKey(schema, tail);

						if (!Contains(id))
						{
							throw new KeyNotFoundException($"State with {id} does not exist");
						}

						var stringValue = Read(id);

						var response = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue);
						value = response.State;

						Delete(id);
					}
					tail++;

					stateQueueInfo.TailKey = tail;

					Console.WriteLine($"Dequeued {value} t:{stateQueueInfo.TailKey} h:{stateQueueInfo.HeadKey}");

					await SetQueueInfo(schema, stateQueueInfo);

					return new ConditionalValue<T>(true, value);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"DequeueAsync for {stateKeyQueueInfo} failed", ex);
				}
			}

			public async Task<ConditionalValue<T>> PeekAsync<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
			{
				var stateKeyQueueInfo = _manager.GetSchemaStateQueueInfoKey(schema);
				var stateQueueInfo = await GetOrAddQueueInfo(schema);
				try
				{
					var tail = stateQueueInfo.TailKey;
					var head = stateQueueInfo.HeadKey;

					if ((tail - head) == 1)
					{
						return new ConditionalValue<T>(false, default(T));
					}


					T value;
					var id = _manager.GetSchemaQueueStateKey(schema, tail);
					lock (_lock)
					{
						if (!Contains(id))
						{
							throw new KeyNotFoundException($"State with {id} does not exist");
						}

						var stringValue = Read(id);

						var response = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue);
						value = response.State;
					}

					return new ConditionalValue<T>(true, value);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"DequeueAsync for {stateKeyQueueInfo} failed", ex);
				}
			}

			public Task CommitAsync()
			{
				return Task.FromResult(true);
			}

			public Task AbortAsync()
			{
				return Task.FromResult(true);
			}

		}

	}
}