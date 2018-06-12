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
    using Microsoft.Azure.Documents.SystemFunctions;

    using Newtonsoft.Json;

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

        private readonly SemaphoreSlim lockSemaphore = new SemaphoreSlim(1);

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
        // ReSharper disable once MemberCanBePrivate.Global - For debugging purposes
        public string InstanceName { get; }

        public IStateQuerySession CreateSession()
        {
            throw new NotImplementedException();
        }

        private async Task<DocumentClient> CreateClientAsync()
        {
            _logger.CreatingClient();

            var client = await _factory.OpenAsync(
                             _databaseName,
                             new CosmosDbCollectionDefinition(_collection, $"/partitionKey"),
                             new Uri(_endpointUri),
                             _collectionPrimaryKey,
                             _connectionPolicySetting);

            if (!_collectionExists)
            {
                await client.EnsureStoreIsConfigured(_databaseName,
                        new CosmosDbCollectionDefinition(_collection, $"/partitionKey"), _logger);

                _collectionExists = true;
            }

            return client;
        }

        private async Task<DocumentClient> GetClientAsync()
        {
            if (this._client != null)
            {
                return this._client;
            }

            try
            {
                await this.lockSemaphore.WaitAsync();

                if (_client == null)
                {
                    this._client = await this.CreateClientAsync();
                }

                return _client;
            }
            finally
            {
                this.lockSemaphore.Release();
            }

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
            private readonly DocumentDbStateSessionManagerWithTransactions _manager;

            public DocumentDbStateSession(DocumentDbStateSessionManagerWithTransactions manager,
                IStateSessionReadOnlyObject[] stateSessionObjects)
                : base(manager, stateSessionObjects)
            {
                _manager = manager;
            }

            public DocumentDbStateSession(DocumentDbStateSessionManagerWithTransactions manager,
                IStateSessionObject[] stateSessionObjects)
                : base(manager, stateSessionObjects)
            {
                _manager = manager;
            }

            Task<DocumentClient> IDocumentDbSession.GetDocumentClientAsync()
            {
                return this._manager.GetClientAsync();
            }

            string IDocumentDbSession.DatabaseName => _manager._databaseName;
            string IDocumentDbSession.DatabaseCollection => _manager._collection;
            PartitionKey IDocumentDbSession.PartitionKey => GetPartitionKey();

            public override string ToString()
            {
                var _documentClient = this.GetDocumentClientAsync().GetAwaiter().GetResult();

                return
                    $"{GetType().Name}\r\n-------------------\r\nManager: {_manager.GetType().Name} {_manager.GetHashCode()}\r\nClient: {_documentClient?.GetType().Name} {_documentClient?.GetHashCode()}\r\nService Name: {_manager.ServiceName}\r\nService Partition: {_manager.PartitionKey}\r\nStorage Partition: {_manager.GetStoragePartitionKey(_manager.ServiceName, _manager.PartitionKey)}";
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

            private Task<DocumentClient> GetDocumentClientAsync()
            {
                return this._manager.GetClientAsync();
            }

            protected override async Task<bool> ContainsInternal(SchemaStateKey key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var id = key.GetId();

                var documentClient = await this.GetDocumentClientAsync();

                try
                {
                    var document = await documentClient.ReadDocumentAsync(this.CreateDocumentUri(id));
                    return true;
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                    {
                        return false;
                    }

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

                    var client = await this.GetDocumentClientAsync();

                    documentCollectionQuery = client.CreateDocumentQuery<IdWrapper>(
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

                            var schemaStateKey = new SchemaStateKey(documentId);
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
                    return new FindByKeyPrefixResult { ContinuationToken = null, Items = results };
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

                var client = await this.GetDocumentClientAsync();

                try
                {
                    var documentCollectionQuery = client.CreateDocumentQuery<StateWrapper>(
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

            protected override  async Task<ConditionalValue<StateWrapper<T>>> TryGetValueInternalAsync<T>(SchemaStateKey key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var id = key.GetId();

                var client = await this.GetDocumentClientAsync();

                try
                {
                    var document = await client.ReadDocumentAsync<StateWrapper<T>>(this.CreateDocumentUri(id), new RequestOptions
                                                                                                                   {
                                                                                                                       PartitionKey = this.GetPartitionKey()
                                                                                                                   });
                    return new ConditionalValue<StateWrapper<T>>(true, document);
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new ConditionalValue<StateWrapper<T>>(false, null);
                    }

                    throw new StateSessionException($"TryGetValueAsync for {id} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"TryGetValueAsync for {id} failed, {ex.Message}", ex);
                }
            }

            protected override async Task<StateWrapper<T>> GetValueInternalAsync<T>(SchemaStateKey key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                try
                {
                    var client = await this.GetDocumentClientAsync();

                    var response = await client.ReadDocumentAsync<StateWrapper<T>>(
                        CreateDocumentUri(key.GetId()),
                        new RequestOptions
                        {
                            PartitionKey = GetPartitionKey()
                        });

                    return response.Document;
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        throw new KeyNotFoundException($"State with {key} does not exist");
                    throw new StateSessionException($"GetValueAsync for {key} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"TryGetValueAsync for {key} failed, {ex.Message}", ex);
                }
            }

            protected override async Task SetValueInternalAsync(SchemaStateKey key, StateWrapper value, Type valueType,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                try
                {

                    var documentClient = await this.GetDocumentClientAsync();

                    await documentClient.UpsertDocumentAsync(
                        CreateDocumentCollectionUri(),
                        value,
                        new RequestOptions
                        {
                            PartitionKey = GetPartitionKey()
                        });
                }
                catch (DocumentClientException dcex)
                {
                    throw new StateSessionException($"SetValueAsync for {key} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"SetValueAsync for {key} failed, {ex.Message}", ex);
                }
            }

            protected override async Task RemoveInternalAsync(SchemaStateKey key,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                var id = key.GetId();
                try
                {
                    var documentClient = await this.GetDocumentClientAsync();

                    await documentClient.DeleteDocumentAsync(CreateDocumentUri(id),
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
                    var documentClient = await this.GetDocumentClientAsync();

                    var resultCount = await documentClient.CreateDocumentQuery<StateWrapper>(
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
                var client = await this.GetClientAsync();
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
            var client = await this.GetClientAsync();

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