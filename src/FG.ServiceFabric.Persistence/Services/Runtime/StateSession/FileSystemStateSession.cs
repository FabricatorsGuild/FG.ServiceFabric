using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using Microsoft.Azure.Documents;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public class FileSystemStateSession : IStateSession
	{
		private readonly object _lock = new object();

		private const string CommonPathDefault = @"c:\temp\servicefabric";
		private readonly string _commonPath;

		private readonly Guid _partitionId;
		private readonly string _partitionKey;
		private readonly string _serviceName;

		public FileSystemStateSession(
			ServiceContext context,
			string storagePath)
		{
			_partitionId = context.PartitionId;
			_partitionKey = StateSessionHelper.GetPartitionInfo(context).GetAwaiter().GetResult();
			_serviceName = context.ServiceTypeName;

			_commonPath = storagePath ?? CommonPathDefault;
			lock (_lock)
			{
				if (!System.IO.Directory.Exists(_commonPath))
				{
					System.IO.Directory.CreateDirectory(_commonPath);
				}
			}
		}


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

		private string GetSchemaStateKey(string schema, string stateName)
		{
			return StateSessionHelper.GetSchemaStateKey(_serviceName, _partitionKey, schema, stateName);
		}
		private string GetSchemaStateQueueInfoKey(string schema)
		{
			return StateSessionHelper.GetSchemaStateQueueInfoKey(_serviceName, _partitionKey, schema);
		}
		private string GetSchemaQueueStateKey(string schema, long index)
		{
			return StateSessionHelper.GetSchemaQueueStateKey(_serviceName, _partitionKey, schema, index);
		}


		public Task OpenDictionary<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}

		public Task OpenQueue<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}

		public Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
		{
			var schemaKey = GetSchemaStateKey(schema, key);
			try
			{
				T value;
				lock (_lock)
				{
					var fileName = $"{_commonPath}\\{schemaKey}.json";
					if (!System.IO.File.Exists(fileName))
					{
						return Task.FromResult(new ConditionalValue<T>(false, default(T)));
					}
					var stringValue = System.IO.File.ReadAllText(fileName);

					var response = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue);
					value = response.State;
				}
				return Task.FromResult(new ConditionalValue<T>(true, value));
			}
			catch (Exception ex)
			{
				throw new StateSessionException($"TryGetValueAsync for {schemaKey} failed", ex);
			}
		}

		public Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
		{
			var schemaKey = GetSchemaStateKey(schema, key);
			try
			{
				T value;
				lock (_lock)
				{
					var fileName = $"{_commonPath}\\{schemaKey}.json";

					if (!System.IO.File.Exists(fileName))
					{
						throw new KeyNotFoundException($"State with {schema}:{key} does not exist");
					}

					var stringValue = System.IO.File.ReadAllText(fileName);

					var response = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue);
					value = response.State;
				}
				return Task.FromResult(value);
			}			
			catch (Exception ex)
			{
				throw new StateSessionException($"TryGetValueAsync for {schemaKey} failed", ex);
			}
		}

		public Task SetValueAsync<T>(string schema, string key, T value, CancellationToken cancellationToken = new CancellationToken())
		{
			var schemaKey = GetSchemaStateKey(schema, key);
			try
			{
				lock (_lock)
				{
					var wrapper = new StateWrapper<T>(key, value, new StateMetadata(schemaKey, _partitionId, _partitionKey));

					var stringValue = Newtonsoft.Json.JsonConvert.SerializeObject(wrapper, new JsonSerializerSettings() { Formatting = Formatting.Indented });

					var fileName = $"{_commonPath}\\{schemaKey}.json";
					if (value == null)
					{
						if (System.IO.File.Exists(fileName))
						{
							System.IO.File.Delete(fileName);
						}
					}
					else
					{
						System.IO.File.WriteAllText(fileName, stringValue);
					}
				}

				return Task.FromResult(true);
			}
			catch (Exception ex)
			{
				throw new StateSessionException($"SetValueAsync for {schemaKey} failed", ex);
			}
		}

		public Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
		{
			var schemaKey = GetSchemaStateKey(schema, key);
			try
			{
				lock (_lock)
				{
					var fileName = $"{_commonPath}\\{schemaKey}.json";
					if (System.IO.File.Exists(fileName))
					{
						System.IO.File.Delete(fileName);
					}
				}

				return Task.FromResult(true);
			}
			catch (DocumentClientException dcex)
			{
				throw new StateSessionException($"SetValueAsync for {schemaKey} failed", dcex);
			}
			catch (Exception ex)
			{
				throw new StateSessionException($"SetValueAsync for {schemaKey} failed", ex);
			}
		}


		private Task<StateWrapperQueueInfo> GetOrAddQueueInfo(string schema)
		{
			var stateKeyQueueInfo = GetSchemaStateQueueInfoKey(schema);
			try
			{
				lock (_lock)
				{
					var fileName = $"{_commonPath}\\{stateKeyQueueInfo}.json";

					if (System.IO.File.Exists(fileName))
					{
						var stringValue = System.IO.File.ReadAllText(fileName);

						var queueInfoResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<StateWrapperQueueInfo>>(stringValue);
						var stateQueueInfo = queueInfoResponse.State;
						return Task.FromResult(stateQueueInfo);
					}

				}

				var value = new StateWrapperQueueInfo()
				{
					HeadKey = 0L,
					TailKey = 0L,
				};

				return SetQueueInfo(schema, value);
			}
			catch (Exception ex)
			{
				throw new StateSessionException($"EnqueueAsync for {stateKeyQueueInfo} failed", ex);
			}
		}
		private Task<StateWrapperQueueInfo> SetQueueInfo(string schema, StateWrapperQueueInfo value)
		{
			var stateKeyQueueInfo = GetSchemaStateQueueInfoKey(schema);
			var stateKey = default(string);
			var tail = 0L;
			var head = 0L;
			try
			{
				lock (_lock)
				{
					var fileName = $"{_commonPath}\\{stateKeyQueueInfo}.json";
					var stateMetadata = new StateMetadata(stateKeyQueueInfo, _partitionId, _partitionKey);
					var document = new StateWrapper<StateWrapperQueueInfo>(stateMetadata.StateName, value, stateMetadata);
					var stringValue = Newtonsoft.Json.JsonConvert.SerializeObject(document, new JsonSerializerSettings(){Formatting = Formatting.Indented});
					System.IO.File.WriteAllText(fileName, stringValue);
				}
				return Task.FromResult(value);
			}
			catch (Exception ex)
			{
				throw new StateSessionException($"CreateQueueInfo for {stateKeyQueueInfo} failed", ex);
			}
		}

		public async Task EnqueueAsync<T>(string schema, T value, CancellationToken cancellationToken = new CancellationToken())
		{
			var stateKeyQueueInfo = GetSchemaStateQueueInfoKey(schema);
			var stateQueueInfo = await GetOrAddQueueInfo(schema);
			try
			{
				lock (_lock)
				{
					var fileName = $"{_commonPath}\\{stateKeyQueueInfo}.json";
					var head = stateQueueInfo.HeadKey;
					head++;
					stateQueueInfo.HeadKey = head;

					var stateKey = GetSchemaQueueStateKey(schema, head);
					var stateMetadata = new StateMetadata(stateKey, _partitionId, _partitionKey);
					var document = new StateWrapper<T>(stateMetadata.StateName, value, stateMetadata);
					var stringValue = Newtonsoft.Json.JsonConvert.SerializeObject(document, new JsonSerializerSettings() { Formatting = Formatting.Indented });
					System.IO.File.WriteAllText(fileName, stringValue);
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
			var stateKeyQueueInfo = GetSchemaStateQueueInfoKey(schema);
			var stateQueueInfo = await GetOrAddQueueInfo(schema);
			try
			{
				var tail = stateQueueInfo.TailKey;
				var head = stateQueueInfo.HeadKey;

				if (tail == head)
				{
					return new ConditionalValue<T>(false, default(T));
				}

				T value;
				lock (_lock)
				{
					var stateKey = GetSchemaQueueStateKey(schema, tail);
					var fileName = $"{_commonPath}\\{stateKey}.json";

					if (!System.IO.File.Exists(fileName))
					{
						throw new KeyNotFoundException($"State with {stateKey} does not exist");
					}

					var stringValue = System.IO.File.ReadAllText(fileName);

					var response = Newtonsoft.Json.JsonConvert.DeserializeObject<StateWrapper<T>>(stringValue);
					value = response.State;

					System.IO.File.Delete(fileName);					
				}
				tail++;
				stateQueueInfo.TailKey = tail;
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
			var stateKeyQueueInfo = GetSchemaStateQueueInfoKey(schema);
			var stateQueueInfo = await GetOrAddQueueInfo(schema);
			try
			{
				var tail = stateQueueInfo.TailKey;
				var head = stateQueueInfo.HeadKey;

				if (tail == head)
				{
					return new ConditionalValue<T>(false, default(T));
				}


				T value;
				var stateKey = GetSchemaQueueStateKey(schema, tail);
				lock (_lock)
				{
					var fileName = $"{_commonPath}\\{stateKey}.json";

					if (!System.IO.File.Exists(fileName))
					{
						throw new KeyNotFoundException($"State with {stateKey} does not exist");
					}

					var stringValue = System.IO.File.ReadAllText(fileName);

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