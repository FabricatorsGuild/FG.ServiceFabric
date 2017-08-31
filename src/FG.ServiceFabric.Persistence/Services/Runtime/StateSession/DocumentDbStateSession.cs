using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public class DocumentDbStateSession : IStateSession
	{
		private readonly string _collection;
		private readonly string _databaseName;
		private readonly DocumentClient _documentClient;
		private readonly Guid _partitionId;
		private readonly string _partitionKey;
		private readonly string _serviceName;

		public DocumentDbStateSession(
			StatefulServiceContext context,
			ISettingsProvider settingsProvider,
			ICosmosDbClientFactory factory = null,
			ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps)
		{
			_partitionId = context.PartitionId;
			_partitionKey = StateSessionHelper.GetPartitionInfo(context).GetAwaiter().GetResult();
			_serviceName = context.ServiceTypeName;

			factory = factory ?? new CosmosDbClientFactory();

			_collection = settingsProvider["Collection"];
			_databaseName = settingsProvider["DatabaseName"];
			var endpointiUri = settingsProvider["EndpointUri"];
			var primaryKey = settingsProvider["PrimaryKey"];

			_documentClient = factory.OpenAsync( // TODO: Do proper init.
				databaseName: _databaseName,
				collection: _collection,
				endpointUri: new Uri(endpointiUri),
				primaryKey: primaryKey,
				connectionPolicySetting: connectionPolicySetting).GetAwaiter().GetResult();

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
			var stateKey = $"{GetSchemaStateKeyPrefix(schema)}{stateName}";
			return stateKey;
		}
		private string GetSchemaStateKeyPrefix(string schema)
		{
			var stateKeyPrefix = $"@@{_serviceName}_{_partitionId}_{schema}_";
			return stateKeyPrefix;
		}
		private string GetSchemaStateQueueInfoKey(string schema)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(schema)}_queue_info";
			return stateKey;
		}
		private string GetSchemaQueueStateKey(string schema, long index)
		{
			var stateKey = $"{GetSchemaStateKeyPrefix(schema)}_{index}";
			return stateKey;
		}


		public Task OpenDictionary<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}

		public Task OpenQueue<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}

		public async Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
		{
			var schemaKey = GetSchemaStateKey(schema, key);
			try
			{
				var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(
					UriFactory.CreateDocumentUri(_databaseName, _collection, schemaKey),
					new RequestOptions {PartitionKey = new PartitionKey(_partitionId)});

				return new ConditionalValue<T>(true, response.Document.State);
			}
			catch (DocumentClientException dcex)
			{
				if (dcex.StatusCode == HttpStatusCode.NotFound)
				{
					return new ConditionalValue<T>(false, default(T));
				}
				throw new StateSessionException($"TryGetValueAsync for {schemaKey} failed", dcex);
			}
			catch (Exception ex)
			{
				throw new StateSessionException($"TryGetValueAsync for {schemaKey} failed", ex);
			}
		}

		public async Task<T> GetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
		{
			var schemaKey = GetSchemaStateKey(schema, key);
			try
			{
				var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(
					UriFactory.CreateDocumentUri(_databaseName, _collection, schemaKey),
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });

				return response.Document.State;
			}
			catch (DocumentClientException dcex)
			{
				if (dcex.StatusCode == HttpStatusCode.NotFound)
				{
					throw new KeyNotFoundException($"State with {schema}:{key} does not exist");
				}
				throw new StateSessionException($"GetValueAsync for {schemaKey} failed", dcex);
			}
			catch (Exception ex)
			{
				throw new StateSessionException($"TryGetValueAsync for {schemaKey} failed", ex);
			}
		}

		public async Task SetValueAsync<T>(string schema, string key, T value, CancellationToken cancellationToken = new CancellationToken())
		{
			var schemaKey = GetSchemaStateKey(schema, key);
			try
			{
				var stateMetadata = new StateMetadata(schemaKey, _partitionId, _partitionKey);
				var document = new StateWrapper<T>(stateMetadata.StateName, value, stateMetadata);
				await _documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseName, _collection), document,
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });
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

		public async Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
		{
			var schemaKey = GetSchemaStateKey(schema, key);
			try
			{
				var stateName = schemaKey;
				await _documentClient.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseName, _collection, schemaKey),
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });
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


		private async Task<StateWrapperQueueInfo> GetOrAddQueueInfo(string schema)
		{
			var stateKeyQueueInfo = GetSchemaStateQueueInfoKey(schema);
			try
			{
				var queueInfoResponse = await _documentClient.ReadDocumentAsync<StateWrapper<StateWrapperQueueInfo>>(
					UriFactory.CreateDocumentUri(_databaseName, _collection, stateKeyQueueInfo),
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });

				var stateQueueInfo = queueInfoResponse.Document.State;
				return stateQueueInfo;
			}
			catch (DocumentClientException dcex)
			{
				if (dcex.StatusCode == HttpStatusCode.NotFound)
				{
					var value = new StateWrapperQueueInfo()
					{
						HeadKey = 0L,
						TailKey = 0L,
					};

					return await SetQueueInfo(schema, value);
				}
				throw new StateSessionException($"DequeueAsync for {stateKeyQueueInfo} failed", dcex);
			}
			catch (Exception ex)
			{
				throw new StateSessionException($"EnqueueAsync for {stateKeyQueueInfo} failed", ex);
			}
		}
		private async Task<StateWrapperQueueInfo> SetQueueInfo(string schema, StateWrapperQueueInfo value)
		{
			var stateKeyQueueInfo = GetSchemaStateQueueInfoKey(schema);
			var stateKey = default(string);
			var tail = 0L;
			var head = 0L;
			try
			{
				var stateMetadata = new StateMetadata(stateKeyQueueInfo, _partitionId, _partitionKey);
				var document = new StateWrapper<StateWrapperQueueInfo>(stateMetadata.StateName, value, stateMetadata);
				await _documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseName, _collection), document,
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });

				return document.State;
			}
			catch (DocumentClientException dcex)
			{
				throw new StateSessionException($"CreateQueueInfo for {stateKeyQueueInfo} failed", dcex);
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
				var head = stateQueueInfo.HeadKey;
				head++;
				stateQueueInfo.HeadKey = head;

				var stateKey = GetSchemaQueueStateKey(schema, head);
				var stateMetadata = new StateMetadata(stateKey, _partitionId, _partitionKey);
				var document = new StateWrapper<T>(stateMetadata.StateName, value, stateMetadata);
				await _documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseName, _collection), document,
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });
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

				var stateKey = GetSchemaQueueStateKey(schema, tail);

				var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(UriFactory.CreateDocumentUri(_databaseName, _collection, stateKey),
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });
				await _documentClient.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseName, _collection, stateKey), 
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });

				tail++;
				stateQueueInfo.TailKey = tail;
				await SetQueueInfo(schema, stateQueueInfo);

				return new ConditionalValue<T>(true, response.Document.State);
			}
			catch (DocumentClientException dcex)
			{
				if (dcex.StatusCode == HttpStatusCode.NotFound)
				{
					return new ConditionalValue<T>(false, default(T));
				}
				throw new StateSessionException($"DequeueAsync for {stateKeyQueueInfo} failed", dcex);
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

				var stateKey = GetSchemaQueueStateKey(schema, tail);

				var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(UriFactory.CreateDocumentUri(_databaseName, _collection, stateKey),
					new RequestOptions { PartitionKey = new PartitionKey(_partitionId) });

				return new ConditionalValue<T>(true, response.Document.State);
			}
			catch (DocumentClientException dcex)
			{
				if (dcex.StatusCode == HttpStatusCode.NotFound)
				{
					return new ConditionalValue<T>(false, default(T));
				}
				throw new StateSessionException($"DequeueAsync for {stateKeyQueueInfo} failed", dcex);
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