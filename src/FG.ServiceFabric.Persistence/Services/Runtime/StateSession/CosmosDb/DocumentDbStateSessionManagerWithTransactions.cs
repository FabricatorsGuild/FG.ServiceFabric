using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession.Internal;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
	public class DocumentDbStateSessionManagerWithTransactions :
		StateSessionManagerBase<DocumentDbStateSessionManagerWithTransactions.DocumentDbStateSession>, 
		IStateSessionManager,
		IStateQuerySessionManager,
		IDocumentDbDataManager
	{
		private readonly string _managerInstance;
		private readonly string _collection;
		private readonly string _collectionPrimaryKey;
		private readonly ConnectionPolicySetting _connectionPolicySetting;
		private readonly string _databaseName;
		private readonly string _endpointUri;
		private bool _collectionExists;

		private readonly ICosmosDbClientFactory _factory;

		public DocumentDbStateSessionManagerWithTransactions(
			string serviceName,
			Guid partitionId,
			string partitionKey,
			ISettingsProvider settingsProvider,
			ICosmosDbClientFactory factory = null,
			ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps) :
			base(serviceName, partitionId, partitionKey)
		{
			_managerInstance = new FG.Common.Utils.MiniId();
			_connectionPolicySetting = connectionPolicySetting;

			_factory = factory ?? new CosmosDbClientFactory();

			_collection = settingsProvider.Collection();
			_databaseName = settingsProvider.DatabaseName();
			_endpointUri = settingsProvider.EndpointUri();
			_collectionPrimaryKey = settingsProvider.PrimaryKey();
		}

		string IDocumentDbDataManager.GetCollectionName()
		{
			return _collection;
		}

		async Task<IDictionary<string, string>> IDocumentDbDataManager.GetCollectionDataAsync(string collectionName)
		{
			try
			{
				var client = await CreateClient();
				var dict = new Dictionary<string, string>();
				var feedResponse = await client.ReadDocumentFeedAsync(UriFactory.CreateDocumentCollectionUri(_databaseName, _collection));

				foreach (var document in feedResponse)
				{
					dict.Add(document.Id, document.ToString());
				}

				return dict;
			}
			catch (DocumentClientException e)
			{
				if (e.Error.Code == "NotFound")
				{
					return null;
				}
				throw;
			}
			catch (Exception e)
			{
				throw;
			}
		}

		async Task IDocumentDbDataManager.CreateCollection(string collectionName)
		{
			var client = await CreateClient();
			await client.EnsureStoreIsConfigured(_databaseName, new CosmosDbCollectionDefinition(_collection, $"/partitionKey"));

			_collectionExists = true;
		}

		async Task IDocumentDbDataManager.DestroyCollecton(string collectionName)
		{
			var client = await CreateClient();
			await client.DestroyCollection(_databaseName, _collection);
		}

		private async Task<DocumentClient> CreateClient()
		{
			var client = await _factory.OpenAsync(
				databaseName: _databaseName,
				collection: new CosmosDbCollectionDefinition(_collection, $"/partitionKey"),
				endpointUri: new Uri(_endpointUri),
				primaryKey: _collectionPrimaryKey,
				connectionPolicySetting: _connectionPolicySetting
			);

			if (!_collectionExists)
			{

				await client.EnsureStoreIsConfigured(_databaseName, new CosmosDbCollectionDefinition(_collection, $"/partitionKey"));

				_collectionExists = true;
			}

			return client;
		}


		public sealed class DocumentDbStateSession : StateSessionManagerBase<DocumentDbStateSessionManagerWithTransactions.DocumentDbStateSession>.StateSessionBase<
			DocumentDbStateSessionManagerWithTransactions>, IStateSession
		{
			private readonly DocumentClient _documentClient;
			private readonly DocumentDbStateSessionManagerWithTransactions _manager;
			private IEnumerable<IStateSessionObject> _attachedObjects;

			public DocumentDbStateSession(DocumentDbStateSessionManagerWithTransactions manager, IStateSessionObject[] stateSessionObjects) 
				: base(manager, stateSessionObjects)
			{
				_manager = manager;
				_documentClient = _manager.CreateClient().GetAwaiter().GetResult();				
			}

			private IStateSessionManagerInternals _managerInternals => _manager;

			private string DatabaseName => _manager._databaseName;
			private string DatabaseCollection => _manager._collection;
			private Guid ServicePartitionId => _manager.PartitionId;
			private string ServicePartitionKey => _manager.PartitionKey;
			private string ServiceTypeName => _manager.ServiceName;

			private Uri CreateDocumentUri(string schemaKey)
			{
				return UriFactory.CreateDocumentUri(DatabaseName, DatabaseCollection, schemaKey);
			}


			protected override Task<bool> ContainsInternal(string id, string schema, string key,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var schemaKey = _managerInternals.GetSchemaStateKey(schema, key);
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
							new FeedOptions()
							{
								PartitionKey = new PartitionKey(ServicePartitionKey),
								MaxItemCount = 1
							})
						.Where(d => d.Id == schemaKey);

					return Task.FromResult(documentCollectionQuery.Any());
				}
				catch (DocumentClientException dcex)
				{
					if (dcex.StatusCode == HttpStatusCode.NotFound)
					{
						return Task.FromResult(false);
					}
					throw new StateSessionException($"ContainsInternal for {id} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"ContainsInternal for {id} failed", ex);
				}
			}

			protected override async Task<FindByKeyPrefixResult> FindByKeyPrefixInternalAsync(string schemaKeyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null, CancellationToken cancellationToken = new CancellationToken())
			{
				var results = new List<string>();
				var resultCount = 0;
				try
				{
					IDocumentQuery<IdWrapper> documentCollectionQuery;
					if (continuationToken?.Marker is string nextToken)
					{
						documentCollectionQuery = _documentClient.CreateDocumentQuery<IdWrapper>(
								UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
								new FeedOptions
								{
									PartitionKey = new PartitionKey(ServicePartitionKey),
									MaxItemCount = maxNumResults,
									RequestContinuation = nextToken,
								})
							.Where(d => d.Id.StartsWith(schemaKeyPrefix))
							.AsDocumentQuery();
					}
					else
					{
						documentCollectionQuery = _documentClient.CreateDocumentQuery<IdWrapper>(
								UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
								new FeedOptions
								{
									PartitionKey = new PartitionKey(ServicePartitionKey),
									MaxItemCount = maxNumResults,
								})
							.Where(d => d.Id.StartsWith(schemaKeyPrefix))
							.AsDocumentQuery();
					}

					while (documentCollectionQuery.HasMoreResults)
					{
						var response = await documentCollectionQuery.ExecuteNextAsync<IdWrapper>(CancellationToken.None);

						foreach (var documentId in response)
						{
							resultCount++;

							var schemaStateKey = StateSessionHelper.SchemaStateKey.Parse(documentId.Id);
							results.Add(schemaStateKey.Key);

							if (resultCount >= maxNumResults)
							{
								var nextContinuationToken = response.ResponseContinuation == null
									? null
									: new ContinuationToken(response.ResponseContinuation);
								return new FindByKeyPrefixResult() { ContinuationToken = nextContinuationToken, Items = results };
							}
						}
					}
					return new FindByKeyPrefixResult() { ContinuationToken = null, Items = results };
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"FindByKeyPrefixAsync for {schemaKeyPrefix} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"FindByKeyPrefixAsync for {schemaKeyPrefix} failed", ex);
				}
			}

			protected override async Task<IEnumerable<string>> EnumerateSchemaNamesInternalAsync(string schemaKeyPrefix, string key,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var results = new List<string>();
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
							new FeedOptions { PartitionKey = new PartitionKey(ServicePartitionKey) })
						.Where(d => d.Id.StartsWith(schemaKeyPrefix))
						.AsDocumentQuery();
				
					while (documentCollectionQuery.HasMoreResults)
					{
						var response = await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

						foreach (var documentId in response)
						{
							if (!results.Contains(documentId.Schema))
							{
								results.Add(documentId.Schema);
							}
						}
					}
					return results;
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"EnumerateSchemaNamesAsync for {key} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"EnumerateSchemaNamesAsync for {key} failed", ex);
				}
			}

			protected override Task<ConditionalValue<StateWrapper<T>>> TryGetValueInternalAsync<T>(string id, string schema, string key,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				var schemaKey = _managerInternals.GetSchemaStateKey(schema, key);
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper<T>>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
							new FeedOptions()
							{
								PartitionKey = new PartitionKey(ServicePartitionKey),
								MaxItemCount = 1
							})
						.Where(d => d.Id == schemaKey);

					foreach (var document in documentCollectionQuery)
					{
						return Task.FromResult(new ConditionalValue<StateWrapper<T>>(true, document));
					}
					return Task.FromResult(new ConditionalValue<StateWrapper<T>>(false, null));
				}
				catch (DocumentClientException dcex)
				{
					if (dcex.StatusCode == HttpStatusCode.NotFound)
					{
						return Task.FromResult(new ConditionalValue<StateWrapper<T>>(false, null));
					}
					throw new StateSessionException($"TryGetValueAsync for {schemaKey} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"TryGetValueAsync for {schemaKey} failed", ex);
				}
			}

			protected override async Task<StateWrapper<T>> GetValueInternalAsync<T>(string id, CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(
						CreateDocumentUri(id),
						new RequestOptions { PartitionKey = new PartitionKey(ServicePartitionKey) });

					return response.Document;
				}
				catch (DocumentClientException dcex)
				{
					if (dcex.StatusCode == HttpStatusCode.NotFound)
					{
						throw new KeyNotFoundException($"State with {id} does not exist");
					}
					throw new StateSessionException($"GetValueAsync for {id} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"TryGetValueAsync for {id} failed", ex);
				}
			}

			protected override async Task SetValueInternalAsync(string id, string schema, string key, StateWrapper value, Type valueType,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					await _documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
						value,
						new RequestOptions { PartitionKey = new PartitionKey(ServicePartitionKey) });
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"SetValueAsync for {id} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"SetValueAsync for {id} failed", ex);
				}
			}

			protected override async Task RemoveInternalAsync(string id, string schema, string key,
				CancellationToken cancellationToken = default(CancellationToken))
			{
				try
				{
					await _documentClient.DeleteDocumentAsync(CreateDocumentUri(id),
						new RequestOptions { PartitionKey = new PartitionKey(ServicePartitionKey) });
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

			protected override async Task<long> GetCountInternalAsync(string schema, CancellationToken cancellationToken)
			{
				try
				{
					var resultCount = await _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
							new FeedOptions { MaxDegreeOfParallelism = -1, PartitionKey = new PartitionKey(ServicePartitionKey) })
						.CountAsync(cancellationToken: cancellationToken);

					return (long)resultCount;
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"GetCountInternalAsync for {schema} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"GetCountInternalAsync for {schema} failed", ex);
				}
			}

			protected override void Dispose(bool disposing)
			{
			}
		}
		
		public IStateQuerySession CreateSession()
		{
			throw new NotImplementedException();
		}

		public string GetCollectionName()
		{
			throw new NotImplementedException();
		}

		public Task<IDictionary<string, string>> GetCollectionDataAsync(string collectionName)
		{
			throw new NotImplementedException();
		}

		public Task CreateCollection(string collectionName)
		{
			throw new NotImplementedException();
		}

		public Task DestroyCollecton(string collectionName)
		{
			throw new NotImplementedException();
		}

		protected override DocumentDbStateSession CreateSessionInternal(StateSessionManagerBase<DocumentDbStateSession> manager, IStateSessionObject[] stateSessionObjects)
		{
			return new DocumentDbStateSession(this, stateSessionObjects);
		}
	}
}