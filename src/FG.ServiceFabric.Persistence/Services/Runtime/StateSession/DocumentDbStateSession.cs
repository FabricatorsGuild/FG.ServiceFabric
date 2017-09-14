using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession
{
	public interface IStateQuerySessionManager
	{
		IStateQuerySession CreateSession();
	}

	public interface IStateQuerySession
	{
		Task<IEnumerable<string>> GetServices();
		Task<IEnumerable<string>> GetPartitions(string service);
		Task<IEnumerable<string>> GetStates(string service, string partition);
		Task<IEnumerable<string>> GetActors(string service, string partition);
		Task<IEnumerable<string>> GetActorReminders(string service, string partition, string actor);
	}

	public class DocumentDbStateSessionManager : StateSessionManagerBase<DocumentDbStateSessionManager.DocumentDbStateSession> , IStateSessionManager, IStateQuerySessionManager
	{
		private readonly string _collection;
		private readonly string _databaseName;
		private readonly string _endpointUri;
		private readonly string _collectionPrimaryKey;
		private readonly ConnectionPolicySetting _connectionPolicySetting;

		private readonly ICosmosDbClientFactory _factory;

		public DocumentDbStateSessionManager(
			string serviceName,
			Guid partitionId,
			string partitionKey,
			ISettingsProvider settingsProvider,
			ICosmosDbClientFactory factory = null,
			ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps) :
			base(serviceName, partitionId, partitionKey)
		{
			_connectionPolicySetting = connectionPolicySetting;

			_factory = factory ?? new CosmosDbClientFactory();

			_collection = settingsProvider["Collection"];
			_databaseName = settingsProvider["DatabaseName"];
			_endpointUri = settingsProvider["EndpointUri"];
			_collectionPrimaryKey = settingsProvider["PrimaryKey"];
		}

		public Task OpenDictionary<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}

		public Task OpenQueue<T>(string schema, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.FromResult(true);
		}
		

		IStateQuerySession IStateQuerySessionManager.CreateSession()
		{
			return new DocumentDbStateQuerySession(this);
		}

		protected override string GetEscapedKey(string key)
		{
			return key;
		}

		protected override string GetUnescapedKey(string key)
		{
			return key;
		}

		protected override DocumentDbStateSession CreateSessionInternal(StateSessionManagerBase<DocumentDbStateSession> manager)
		{
			return new DocumentDbStateSession(this);
		}

		public sealed class DocumentDbStateSession : IStateSession
		{
			private readonly DocumentDbStateSessionManager _manager;
			private readonly DocumentClient _documentClient;

			public DocumentDbStateSession(
				DocumentDbStateSessionManager manager)
			{
				_manager = manager;
				_documentClient = _manager._factory.OpenAsync( // TODO: Do proper init.
					databaseName: _manager._databaseName,
					collection: new CosmosDbCollectionDefinition(_manager._collection, $"/partitionKey"),
					endpointUri: new Uri(_manager._endpointUri),
					primaryKey: _manager._collectionPrimaryKey,
					connectionPolicySetting: _manager._connectionPolicySetting).GetAwaiter().GetResult();
			}

			private Uri CreateDocumentUri(string schemaKey)
			{
				return UriFactory.CreateDocumentUri(DatabaseName, DatabaseCollection, schemaKey);
			}			

			private string DatabaseName => _manager._databaseName;
			private string DatabaseCollection => _manager._collection;
			private Guid ServicePartitionId => _manager.PartitionId;
			private string ServicePartitionKey => _manager.PartitionKey;
			private string ServiceTypeName => _manager.ServiceName;

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				if (disposing)
				{

				}
			}

			public Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				var schemaKey = _manager.GetSchemaStateKey(schema, key);
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
						return Task.FromResult(new ConditionalValue<T>(true, document.State));
					}
					return Task.FromResult(new ConditionalValue<T>(false, default(T)));
				}
				catch (DocumentClientException dcex)
				{
					if (dcex.StatusCode == HttpStatusCode.NotFound)
					{
						return Task.FromResult(new ConditionalValue<T>(false, default(T)));
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
				var schemaKey = _manager.GetSchemaStateKey(schema, key);
				try
				{
					var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(
						CreateDocumentUri(schemaKey),
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

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

			public async Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata, CancellationToken cancellationToken = new CancellationToken())
			{
				var id = _manager.GetSchemaStateKey(schema, key);
				try
				{
					var document = _manager.BuildWrapperGeneric(metadata, id, schema, key, value);					
					await _documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection), document,
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});
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

			public async Task SetValueAsync(string schema, string key, Type valueType, object value, IValueMetadata metadata, CancellationToken cancellationToken = new CancellationToken())
			{
				var id = _manager.GetSchemaStateKey(schema, key);
				try
				{
					var wrapper = _manager.CallGenericMethod(nameof(_manager.BuildWrapperGeneric), new Type[] { valueType }, metadata, id, schema, key, value);
					await _documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection), wrapper,
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});
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

			public Task RemoveAsync<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				return RemoveAsync(schema, key, cancellationToken);
			}

			public async Task RemoveAsync(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				var schemaKey = _manager.GetSchemaStateKey(schema, key);
				try
				{
					await _documentClient.DeleteDocumentAsync(CreateDocumentUri(schemaKey),
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"RemoveAsync for {schemaKey} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"RemoveAsync for {schemaKey} failed", ex);
				}
			}

			private async Task<QueueInfo> GetOrAddQueueInfo(string schema)
			{
				var stateKeyQueueInfo = _manager.GetSchemaStateQueueInfoKey(schema);
				try
				{
					var queueInfoResponse = await _documentClient.ReadDocumentAsync<StateWrapper<QueueInfo>>(
						UriFactory.CreateDocumentUri(DatabaseName, DatabaseCollection, stateKeyQueueInfo),
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

					var stateQueueInfo = queueInfoResponse.Document.State;
					return stateQueueInfo;
				}
				catch (DocumentClientException dcex)
				{
					if (dcex.StatusCode == HttpStatusCode.NotFound)
					{
						var value = new QueueInfo()
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

			private async Task<QueueInfo> SetQueueInfo(string schema, QueueInfo value)
			{
				var id = _manager.GetSchemaStateQueueInfoKey(schema);
				var key = StateSessionHelper.ReliableStateQueueInfoName;
				try
				{
					var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
					var document = _manager.BuildWrapperGeneric(metadata, id, schema, key, value);
					await _documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection), document,
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

					return document.State;
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"CreateQueueInfo for {id} failed", dcex);
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
					var head = stateQueueInfo.HeadKey;
					head++;
					stateQueueInfo.HeadKey = head;

					var key = head.ToString();
					var id = _manager.GetSchemaQueueStateKey(schema, head);
					var document = _manager.BuildWrapperGeneric(metadata, id, schema, key, value);
					await _documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection), document,
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});
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

					if (tail == head)
					{
						return new ConditionalValue<T>(false, default(T));
					}

					var stateKey = _manager.GetSchemaQueueStateKey(schema, tail);

					var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(CreateDocumentUri(DatabaseName),
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionId)});
					await _documentClient.DeleteDocumentAsync(CreateDocumentUri(stateKey),
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

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
				var stateKeyQueueInfo = _manager.GetSchemaStateQueueInfoKey(schema);
				var stateQueueInfo = await GetOrAddQueueInfo(schema);
				try
				{
					var tail = stateQueueInfo.TailKey;
					var head = stateQueueInfo.HeadKey;

					if (tail == head)
					{
						return new ConditionalValue<T>(false, default(T));
					}

					var stateKey = _manager.GetSchemaQueueStateKey(schema, tail);

					var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(CreateDocumentUri(stateKey),
						new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

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

			public Task<bool> Contains<T>(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				return Contains(schema, key, cancellationToken);
			}

			public Task<bool> Contains(string schema, string key, CancellationToken cancellationToken = new CancellationToken())
			{
				var schemaKey = _manager.GetSchemaStateKey(schema, key);
				try
				{
					var docExists = _documentClient.CreateDocumentQuery(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
							new FeedOptions()
							{
								PartitionKey = new PartitionKey(ServicePartitionKey),
								MaxItemCount = 1
							})
						.Where(d => d.Id == schemaKey);

					foreach (var document in docExists)
					{
						return Task.FromResult(true);
					}
					return Task.FromResult(false);
				}
				catch (DocumentClientException dcex)
				{
					if (dcex.StatusCode == HttpStatusCode.NotFound)
					{
						return Task.FromResult(false);
					}
					throw new StateSessionException($"Contains for {schemaKey} failed", dcex);
				}
				catch (Exception ex)
				{
					throw new StateSessionException($"Contains for {schemaKey} failed", ex);
				}
			}

			public Task<FindByKeyPrefixResult> FindByKeyPrefixAsync<T>(string schema, string keyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken())
			{

				return FindByKeyPrefixAsync(schema, keyPrefix, maxNumResults, continuationToken, cancellationToken);
			}

			public async Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string schema, string keyPrefix, int maxNumResults = 100000,
				ContinuationToken continuationToken = null,
				CancellationToken cancellationToken = new CancellationToken())
			{
				var results = new List<string>();
				var resultCount = 0;
				var schemaKeyPrefix = _manager.GetSchemaStateKey(schema, keyPrefix);
				try
				{
					IDocumentQuery<IdWrapper> documentCollectionQuery;
					var nextToken = continuationToken?.Marker as string;
					if (nextToken != null)
					{
						documentCollectionQuery = _documentClient.CreateDocumentQuery<IdWrapper>(
								UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
								new FeedOptions
								{
									PartitionKey = new PartitionKey(ServicePartitionKey),
									MaxItemCount = maxNumResults,
								})
							.Where(d => d.Id.StartsWith(schemaKeyPrefix) && string.Compare(d.Id, nextToken, StringComparison.Ordinal) > 0)
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

							if (resultCount > maxNumResults)
							{
								return new FindByKeyPrefixResult() {ContinuationToken = new ContinuationToken(documentId.Id), Items = results};
							}
						}
					}
					return new FindByKeyPrefixResult() {ContinuationToken = null, Items = results};
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

			public async Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key, CancellationToken cancellationToken = new CancellationToken())
			{
				var results = new List<string>();
				var resultCount = 0;
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
							new FeedOptions {PartitionKey = new PartitionKey(ServicePartitionKey)})
						.Where(d => d.Id == key)
						.AsDocumentQuery();

					while (documentCollectionQuery.HasMoreResults)
					{
						var response = await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

						foreach (var documentId in response)
						{
							resultCount++;
							results.Add(documentId.Id);
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

		}

		public class DocumentDbStateQuerySession : IStateQuerySession
		{
			private readonly DocumentDbStateSessionManager _manager;
			private readonly DocumentClient _documentClient;

			public DocumentDbStateQuerySession(
				DocumentDbStateSessionManager manager)
			{
				_manager = manager;
				_documentClient = _manager._factory.OpenAsync( // TODO: Do proper init.
					databaseName: _manager._databaseName,
					collection: new CosmosDbCollectionDefinition(_manager._collection, $"/partitionKey"),
					endpointUri: new Uri(_manager._endpointUri),
					primaryKey: _manager._collectionPrimaryKey,
					connectionPolicySetting: _manager._connectionPolicySetting).GetAwaiter().GetResult();
			}


			private Uri CreateDocumentUri(string schemaKey)
			{
				return UriFactory.CreateDocumentUri(DatabaseName, DatabaseCollection, schemaKey);
			}

			private string DatabaseName => _manager._databaseName;
			private string DatabaseCollection => _manager._collection;
			private Guid ServicePartitionId => _manager.PartitionId;
			private string ServicePartitionKey => _manager.PartitionKey;
			private string ServiceTypeName => _manager.ServiceName;

			public async Task<IEnumerable<string>> GetServices()
			{
				var results = new List<string>();
				var resultCount = 0;
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection))
						.AsDocumentQuery();

					while (documentCollectionQuery.HasMoreResults)
					{
						var response = await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

						foreach (var documentId in response)
						{
							resultCount++;
							if (!results.Contains(documentId.ServiceTypeName))
							{
								results.Add(documentId.ServiceTypeName);
							}
						}
					}
					return results;
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"Failed to GetServices {dcex.Message}", dcex);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Failed to GetServices {ex.Message}");
					throw new StateSessionException($"Failed to GetServices {ex.Message}", ex);
				}
			}

			public async Task<IEnumerable<string>> GetPartitions(string service)
			{
				var results = new List<string>();
				var resultCount = 0;
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection))
						.AsDocumentQuery();

					while (documentCollectionQuery.HasMoreResults)
					{
						var response = await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

						foreach (var documentId in response)
						{
							resultCount++;
							if (!results.Contains(documentId.PartitionKey))
							{
								results.Add(documentId.PartitionKey);
							}
						}
					}
					return results;
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"Failed to GetPartitions {dcex.Message}", dcex);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Failed to GetPartitions {ex.Message}");
					throw new StateSessionException($"Failed to GetPartitions {ex.Message}", ex);
				}
			}

			public async Task<IEnumerable<string>> GetStates(string service, string partition)
			{
				var results = new List<string>();
				var resultCount = 0;
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection))
						.AsDocumentQuery();

					while (documentCollectionQuery.HasMoreResults)
					{
						var response = await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

						foreach (var documentId in response)
						{
							resultCount++;
							var schemaKey = $"{documentId.Schema}_{documentId.Key}";
							if (!results.Contains(schemaKey))
							{
								results.Add(schemaKey);
							}
						}
					}
					return results;
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"Failed to GetStates {dcex.Message}", dcex);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Failed to GetStates {ex.Message}");
					throw new StateSessionException($"Failed to GetStates {ex.Message}", ex);
				}
			}

			public async Task<IEnumerable<string>> GetActors(string service, string partition)
			{
				var results = new List<string>();
				var resultCount = 0;
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection))
						.AsDocumentQuery();

					while (documentCollectionQuery.HasMoreResults)
					{
						var response = await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

						foreach (var documentId in response)
						{
							resultCount++;
							if (!results.Contains(documentId.Key))
							{
								results.Add(documentId.Key);
							}
						}
					}
					return results;
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"Failed to GetPartitions {dcex.Message}", dcex);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Failed to GetStates {ex.Message}");
					throw new StateSessionException($"Failed to GetServices {ex.Message}", ex);
				}
			}

			public async Task<IEnumerable<string>> GetActorReminders(string service, string partition, string actor)
			{
				var results = new List<string>();
				var resultCount = 0;
				try
				{
					var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
							UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection))
						.AsDocumentQuery();

					while (documentCollectionQuery.HasMoreResults)
					{
						var response = await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

						foreach (var documentId in response)
						{
							resultCount++;
							if (!results.Contains(documentId.Key))
							{
								results.Add(documentId.Key);
							}
						}
					}
					return results;
				}
				catch (DocumentClientException dcex)
				{
					throw new StateSessionException($"Failed to GetPartitions {dcex.Message}", dcex);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Failed to GetStates {ex.Message}");
					throw new StateSessionException($"Failed to GetServices {ex.Message}", ex);
				}
			}
		}
	}
}