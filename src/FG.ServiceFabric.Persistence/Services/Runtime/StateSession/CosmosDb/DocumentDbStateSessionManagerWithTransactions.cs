using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
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
        private readonly string _collection;
        private readonly string _collectionPrimaryKey;
        private readonly ConnectionPolicySetting _connectionPolicySetting;
        private readonly string _databaseName;
        private readonly string _endpointUri;

        private readonly ICosmosDbClientFactory _factory;
        private readonly IDocumentDbStateSessionManagerLogger _logger;

        private DocumentClient _client;
        private bool _collectionExists;
        private readonly object _lock = new object();

        public DocumentDbStateSessionManagerWithTransactions(
            string serviceName,
            Guid partitionId,
            string partitionKey,
            ISettingsProvider settingsProvider,
            ICosmosDbClientFactory factory = null,
            ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps) :
            base(serviceName, partitionId, partitionKey)
        {
            InstanceName = new MiniId();
            _logger = new DocumentDbStateSessionManagerLogger(InstanceName);
            _connectionPolicySetting = connectionPolicySetting;

            _factory = factory ?? new CosmosDbClientFactory();

            _collection = settingsProvider.Collection();
            _databaseName = settingsProvider.DatabaseName();
            _endpointUri = settingsProvider.EndpointUri();
            _collectionPrimaryKey = settingsProvider.PrimaryKey();

            _logger.StartingManager(serviceName, partitionId, partitionKey, _endpointUri, _databaseName, _collection);
        }

        // ReSharper disable once UnusedMember.Global - For debugging purposes
        public string InstanceName { get; }

        public IStateQuerySession CreateSession()
        {
            throw new NotImplementedException();
        }

        private DocumentClient CreateClient()
        {
            _logger.CreatingClient();
            lock (_lock)
            {
                _client = _factory.OpenAsync(
                    _databaseName,
                    new CosmosDbCollectionDefinition(_collection, $"/partitionKey"),
                    new Uri(_endpointUri),
                    _collectionPrimaryKey,
                    _connectionPolicySetting
                ).GetAwaiter().GetResult();


                if (!_collectionExists)
                {
                    _client.EnsureStoreIsConfigured(_databaseName,
                            new CosmosDbCollectionDefinition(_collection, $"/partitionKey"), _logger).GetAwaiter()
                        .GetResult();
                    _collectionExists = true;
                }
            }

            return _client;
        }

        private DocumentClient GetClient()
        {
            if (_client == null)
                _client = CreateClient();

            return _client;
        }

        protected override DocumentDbStateSession CreateSessionInternal(
            StateSessionManagerBase<DocumentDbStateSession> manager,
            IStateSessionObject[] stateSessionObjects)
        {
            return new DocumentDbStateSession(this, stateSessionObjects);
        }

        protected override DocumentDbStateSession CreateSessionInternal(
            StateSessionManagerBase<DocumentDbStateSession> manager,
            IStateSessionReadOnlyObject[] stateSessionObjects)
        {
            return new DocumentDbStateSession(this, stateSessionObjects);
        }

        public class DocumentDbStateSession : StateSessionBase<
            DocumentDbStateSessionManagerWithTransactions>, IDocumentDbSession
        {
            private readonly DocumentClient _documentClient;
            private readonly DocumentDbStateSessionManagerWithTransactions _manager;

            public DocumentDbStateSession(DocumentDbStateSessionManagerWithTransactions manager,
                IStateSessionReadOnlyObject[] stateSessionObjects)
                : base(manager, stateSessionObjects)
            {
                _manager = manager;
                _documentClient = _manager.GetClient();
            }

            public DocumentDbStateSession(DocumentDbStateSessionManagerWithTransactions manager,
                IStateSessionObject[] stateSessionObjects)
                : base(manager, stateSessionObjects)
            {
                _manager = manager;
                _documentClient = _manager.GetClient();
            }

            DocumentClient IDocumentDbSession.Client => _documentClient;
            string IDocumentDbSession.DatabaseName => _manager._databaseName;
            string IDocumentDbSession.DatabaseCollection => _manager._collection;
            PartitionKey IDocumentDbSession.PartitionKey => GetPartitionKey();

            public override string ToString()
            {
                return
                    $"{GetType().Name}\r\n-------------------\r\nManager: {_manager.GetType().Name} {_manager.GetHashCode()}\r\nClient: {_documentClient.GetType().Name} {_documentClient.GetHashCode()}\r\nService Name: {_manager.ServiceName}\r\nService Partition: {_manager.PartitionKey}\r\nStorage Partition: {_manager.GetStoragePartitionKey(_manager.ServiceName, _manager.PartitionKey)}";
            }

            private PartitionKey GetPartitionKey()
            {
                var compoundKey = _manager.GetStoragePartitionKey(_manager.ServiceName, _manager.PartitionKey);
                return new PartitionKey(compoundKey);
            }

            private Uri CreateDocumentUri(string schemaKey)
            {
                return UriFactory.CreateDocumentUri(_manager._databaseName, _manager._collection, schemaKey);
            }

            private Uri CreateDocumentCollectionUri()
            {
                return UriFactory.CreateDocumentCollectionUri(_manager._databaseName, _manager._collection);
            }

            protected override Task<bool> ContainsInternal(SchemaStateKey id,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                try
                {
                    var documentCollectionQuery = _documentClient.CreateDocumentQuery<IdWrapper>(
                            CreateDocumentCollectionUri(),
                            new FeedOptions
                            {
                                PartitionKey = GetPartitionKey(),
                                MaxItemCount = 1
                            })
                        .Where(d => d.Id == id);

                    // ReSharper disable once UnusedVariable - cannot use Any() on DocumentQueryException
                    foreach (var document in documentCollectionQuery)
                        return Task.FromResult(true);
                    return Task.FromResult(false);
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        return Task.FromResult(false);
                    throw new StateSessionException($"ContainsInternal for {id} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"ContainsInternal for {id} failed, {ex.Message}", ex);
                }
            }

            protected override async Task<FindByKeyPrefixResult> FindByKeyPrefixInternalAsync(string schemaKeyPrefix,
                int maxNumResults = 100000,
                ContinuationToken continuationToken = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var results = new List<string>();
                var resultCount = 0;
                try
                {
                    IDocumentQuery<IdWrapper> documentCollectionQuery;
                    var nextToken = continuationToken?.Marker as string;

                    documentCollectionQuery = _documentClient.CreateDocumentQuery<IdWrapper>(
                            CreateDocumentCollectionUri(),
                            new FeedOptions
                            {
                                PartitionKey = GetPartitionKey(),
                                MaxItemCount = maxNumResults,
                                RequestContinuation = nextToken
                            })
                        .Where(d => d.Id.StartsWith(schemaKeyPrefix))
                        .AsDocumentQuery();

                    while (documentCollectionQuery.HasMoreResults)
                    {
                        var response =
                            await documentCollectionQuery.ExecuteNextAsync<IdWrapper>(CancellationToken.None);

                        foreach (var documentId in response)
                        {
                            resultCount++;

                            var schemaStateKey = SchemaStateKey.Parse(documentId.Id);
                            results.Add(schemaStateKey.Key);

                            if (resultCount >= maxNumResults)
                            {
                                var nextContinuationToken = response.ResponseContinuation == null
                                    ? null
                                    : new ContinuationToken(response.ResponseContinuation);
                                return new FindByKeyPrefixResult
                                {
                                    ContinuationToken = nextContinuationToken,
                                    Items = results
                                };
                            }
                        }
                    }
                    return new FindByKeyPrefixResult {ContinuationToken = null, Items = results};
                }
                catch (DocumentClientException dcex)
                {
                    throw new StateSessionException(
                        $"FindByKeyPrefixAsync for {schemaKeyPrefix} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"FindByKeyPrefixAsync for {schemaKeyPrefix} failed, {ex.Message}",
                        ex);
                }
            }

            protected override async Task<IEnumerable<string>> EnumerateSchemaNamesInternalAsync(string schemaKeyPrefix,
                string key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var results = new List<string>();
                try
                {
                    var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper>(
                            CreateDocumentCollectionUri(),
                            new FeedOptions
                            {
                                PartitionKey = GetPartitionKey()
                            })
                        .Where(d => d.Id.StartsWith(schemaKeyPrefix))
                        .AsDocumentQuery();

                    while (documentCollectionQuery.HasMoreResults)
                    {
                        var response =
                            await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

                        foreach (var documentId in response)
                            if (!results.Contains(documentId.Schema))
                                results.Add(documentId.Schema);
                    }
                    return results;
                }
                catch (DocumentClientException dcex)
                {
                    throw new StateSessionException($"EnumerateSchemaNamesAsync for {key} failed, {dcex.Message}",
                        dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"EnumerateSchemaNamesAsync for {key} failed, {ex.Message}", ex);
                }
            }

            protected override Task<ConditionalValue<StateWrapper<T>>> TryGetValueInternalAsync<T>(SchemaStateKey id,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                try
                {
                    var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper<T>>(
                            CreateDocumentCollectionUri(),
                            new FeedOptions
                            {
                                PartitionKey = GetPartitionKey(),
                                MaxItemCount = 1
                            })
                        .Where(d => d.Id == id);

                    foreach (var document in documentCollectionQuery)
                        return Task.FromResult(new ConditionalValue<StateWrapper<T>>(true, document));
                    return Task.FromResult(new ConditionalValue<StateWrapper<T>>(false, null));
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        return Task.FromResult(new ConditionalValue<StateWrapper<T>>(false, null));
                    throw new StateSessionException($"TryGetValueAsync for {id} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"TryGetValueAsync for {id} failed, {ex.Message}", ex);
                }
            }

            protected override async Task<StateWrapper<T>> GetValueInternalAsync<T>(string id,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                try
                {
                    var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(
                        CreateDocumentUri(id),
                        new RequestOptions
                        {
                            PartitionKey = GetPartitionKey()
                        });

                    return response.Document;
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        throw new KeyNotFoundException($"State with {id} does not exist");
                    throw new StateSessionException($"GetValueAsync for {id} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"TryGetValueAsync for {id} failed, {ex.Message}", ex);
                }
            }

            protected override async Task SetValueInternalAsync(SchemaStateKey id, StateWrapper value, Type valueType,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                try
                {
                    await _documentClient.UpsertDocumentAsync(
                        CreateDocumentCollectionUri(),
                        value,
                        new RequestOptions
                        {
                            PartitionKey = GetPartitionKey()
                        });
                }
                catch (DocumentClientException dcex)
                {
                    throw new StateSessionException($"SetValueAsync for {id} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"SetValueAsync for {id} failed, {ex.Message}", ex);
                }
            }

            protected override async Task RemoveInternalAsync(SchemaStateKey id,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                try
                {
                    await _documentClient.DeleteDocumentAsync(CreateDocumentUri(id),
                        new RequestOptions
                        {
                            PartitionKey = GetPartitionKey()
                        });
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        throw new KeyNotFoundException($"RemoveAsync for {id} failed, the key was not found");
                    throw new StateSessionException($"RemoveAsync for {id} failed", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"RemoveAsync for {id} failed", ex);
                }
            }

            protected override async Task<long> GetCountInternalAsync(string schema,
                CancellationToken cancellationToken)
            {
                try
                {
                    var resultCount = await _documentClient.CreateDocumentQuery<StateWrapper>(
                            CreateDocumentCollectionUri(),
                            new FeedOptions
                            {
                                MaxDegreeOfParallelism = -1,
                                PartitionKey = GetPartitionKey()
                            })
                        .CountAsync(cancellationToken);

                    return resultCount;
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

        #region Data Manager

        string IDocumentDbDataManager.GetCollectionName()
        {
            return _collection;
        }

        async Task<IDictionary<string, string>> IDocumentDbDataManager.GetCollectionDataAsync(string collectionName)
        {
            try
            {
                var client = GetClient();
                var dict = new Dictionary<string, string>();
                var feedResponse =
                    await client.ReadDocumentFeedAsync(
                        UriFactory.CreateDocumentCollectionUri(_databaseName, collectionName));

                foreach (var document in feedResponse)
                    dict.Add(document.Id, document.ToString());

                return dict;
            }
            catch (DocumentClientException e)
            {
                if (e.Error.Code == "NotFound")
                    return null;
                throw;
            }
            catch (Exception)
            {
                // TODO: inject logger and log this
                throw;
            }
        }

        async Task IDocumentDbDataManager.CreateCollection(string collectionName)
        {
            var client = GetClient();

            await client.EnsureStoreIsConfigured(_databaseName,
                new CosmosDbCollectionDefinition(_collection, $"/partitionKey"), _logger);

            _collectionExists = true;
        }

        async Task IDocumentDbDataManager.DestroyCollecton(string collectionName)
        {
            var client = await _factory.OpenAsync(
                _databaseName,
                new CosmosDbCollectionDefinition(_collection, $"/partitionKey"),
                new Uri(_endpointUri),
                _collectionPrimaryKey,
                _connectionPolicySetting
            );
            await client.DestroyCollection(_databaseName, _collection);
        }

        #endregion
    }
}