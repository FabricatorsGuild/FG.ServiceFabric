using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Services.Runtime.StateSession.Internal;
using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Data;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
    [Obsolete("Use DocumentDbStateSessionWithTransactions instead", true)]
    public class DocumentDbStateSessionManager :
        StateSessionManagerBase<DocumentDbStateSessionManager.DocumentDbStateSession>, IStateSessionManager,
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
        private bool _collectionExists;

        public DocumentDbStateSessionManager(
            string serviceName,
            Guid partitionId,
            string partitionKey,
            ISettingsProvider settingsProvider,
            ICosmosDbClientFactory factory = null,
            ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps,
            ILoggerFactory loggerFactory = null) :
            base(serviceName, partitionId, partitionKey)
        {
            InstanceName = new MiniId();

            _logger = loggerFactory?.CreateLogger(this) ?? new DocumentDbStateSessionManagerLogger(InstanceName);
            _connectionPolicySetting = connectionPolicySetting;

            _factory = factory ?? new CosmosDbClientFactory();

            _collection = settingsProvider.Collection();
            _databaseName = settingsProvider.DatabaseName();
            _endpointUri = settingsProvider.EndpointUri();
            _collectionPrimaryKey = settingsProvider.PrimaryKey();
        }

        // ReSharper disable once UnusedMember.Global - used for debugging and loggig
        public string InstanceName { get; }

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
                var feedResponse =
                    await client.ReadDocumentFeedAsync(
                        UriFactory.CreateDocumentCollectionUri(_databaseName, _collection));

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
            var client = await CreateClient();
            await client.EnsureStoreIsConfigured(_databaseName,
                new CosmosDbCollectionDefinition(_collection, $"/partitionKey"), _logger);

            _collectionExists = true;
        }

        async Task IDocumentDbDataManager.DestroyCollecton(string collectionName)
        {
            var client = await CreateClient();
            await client.DestroyCollection(_databaseName, _collection);
        }

        IStateQuerySession IStateQuerySessionManager.CreateSession()
        {
            return new DocumentDbStateQuerySession(this);
        }

        private async Task<DocumentClient> CreateClient()
        {
            var client = await _factory.OpenAsync(
                _databaseName,
                new CosmosDbCollectionDefinition(_collection, $"/partitionKey"),
                new Uri(_endpointUri),
                _collectionPrimaryKey,
                _connectionPolicySetting
            );

            if (!_collectionExists)
            {
                await client.EnsureStoreIsConfigured(_databaseName,
                    new CosmosDbCollectionDefinition(_collection, $"/partitionKey"), _logger);

                _collectionExists = true;
            }

            return client;
        }

        protected override DocumentDbStateSession CreateSessionInternal(
            StateSessionManagerBase<DocumentDbStateSession> manager,
            IStateSessionObject[] stateSessionObjects)
        {
            return new DocumentDbStateSession(this, false, stateSessionObjects);
        }

        protected override DocumentDbStateSession CreateSessionInternal(
            StateSessionManagerBase<DocumentDbStateSession> manager,
            IStateSessionReadOnlyObject[] stateSessionObjects)
        {
            return new DocumentDbStateSession(this, true, stateSessionObjects);
        }

        public sealed class DocumentDbStateSession : IStateSession
        {
            private readonly DocumentClient _documentClient;
            private readonly IDocumentDbStateSessionLogger _logger;
            private readonly DocumentDbStateSessionManager _manager;
            private readonly string _sessionInstance;
            private IEnumerable<IStateSessionReadOnlyObject> _attachedObjects;

            public DocumentDbStateSession(
                DocumentDbStateSessionManager manager,
                bool readOnly,
                IStateSessionReadOnlyObject[] stateSessionObjects,
                ILoggerFactory loggerFactory = null)
            {
                _manager = manager;
                IsReadOnly = readOnly;
                _sessionInstance = new MiniId();

                _logger = loggerFactory?.CreateLogger(_manager, this) ??
                          new DocumentDbStateSessionLogger(_manager.InstanceName, _sessionInstance);

                _logger.StartingSession(stateSessionObjects.AsList());
                _documentClient = _manager.CreateClient().GetAwaiter().GetResult();

                AttachObjects(stateSessionObjects);
            }

            public bool IsReadOnly { get; }

            private IStateSessionManagerInternals _managerInternals => _manager;

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

            public Task<ConditionalValue<T>> TryGetValueAsync<T>(string schema, string key,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var internalKey = _managerInternals.GetKey(new DictionaryStateKey(schema, key));
                var id = internalKey.GetId();
                try
                {
                    var documentCollectionQuery = _documentClient.CreateDocumentQuery<StateWrapper<T>>(
                            UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                            new FeedOptions
                            {
                                PartitionKey = new PartitionKey(ServicePartitionKey),
                                MaxItemCount = 1
                            })
                        .Where(d => d.Id == id);

                    foreach (var document in documentCollectionQuery)
                        return Task.FromResult(new ConditionalValue<T>(true, document.State));
                    return Task.FromResult(new ConditionalValue<T>(false, default(T)));
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        return Task.FromResult(new ConditionalValue<T>(false, default(T)));
                    _logger.DocumentClientException(nameof(TryGetValueAsync), (int?) dcex.StatusCode ?? -1, dcex.Error,
                        dcex.Message);
                    throw new DocumentDbStateSessionExceptionException(
                        $"TryGetValueAsync for {id} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"TryGetValueAsync for {id} failed, {ex.Message}", ex);
                }
            }

            public async Task<T> GetValueAsync<T>(string schema, string key,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var id = _managerInternals.GetKey(new DictionaryStateKey(schema, key)).GetId();
                try
                {
                    var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(
                        CreateDocumentUri(id),
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

                    return response.Document.State;
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        throw new KeyNotFoundException($"State with {id} does not exist");
                    _logger.DocumentClientException(nameof(GetValueAsync), (int?) dcex.StatusCode ?? -1, dcex.Error,
                        dcex.Message);
                    throw new DocumentDbStateSessionExceptionException(
                        $"GetValueAsync for {id} ({schema}:{key}) failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"TryGetValueAsync for {id} ({schema}:{key}) failed, {ex.Message}",
                        ex);
                }
            }

            public async Task SetValueAsync<T>(string schema, string key, T value, IValueMetadata metadata,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var id = _managerInternals.GetKey(new DictionaryStateKey(schema, key));
                try
                {
                    var document = _managerInternals.BuildWrapperGeneric(metadata, id, value);
                    await _documentClient.UpsertDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                        document,
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});
                }
                catch (DocumentClientException dcex)
                {
                    _logger.DocumentClientException(nameof(SetValueAsync), (int?) dcex.StatusCode ?? -1, dcex.Error,
                        dcex.Message);
                    throw new DocumentDbStateSessionExceptionException(
                        $"SetValueAsync for {id} ({schema}:{key}) failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"SetValueAsync for {id} ({schema}:{key}) failed, {ex.Message}",
                        ex);
                }
            }

            public async Task SetValueAsync(string schema, string key, Type valueType, object value,
                IValueMetadata metadata,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var id = _managerInternals.GetKey(new DictionaryStateKey(schema, key));
                try
                {
                    var wrapper = _managerInternals.BuildWrapper(metadata, id, valueType, value);
                    await _documentClient.UpsertDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                        wrapper,
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});
                }
                catch (DocumentClientException dcex)
                {
                    _logger.DocumentClientException(nameof(SetValueAsync), (int?) dcex.StatusCode ?? -1, dcex.Error,
                        dcex.Message);
                    throw new DocumentDbStateSessionExceptionException($"SetValueAsync for {id} failed, {dcex.Message}",
                        dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"SetValueAsync for {id} failed, {ex.Message}", ex);
                }
            }

            public Task RemoveAsync<T>(string schema, string key,
                CancellationToken cancellationToken = new CancellationToken())
            {
                return RemoveAsync(schema, key, cancellationToken);
            }

            public async Task RemoveAsync(string schema, string key,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var id = _managerInternals.GetKey(new DictionaryStateKey(schema, key)).GetId();
                try
                {
                    await _documentClient.DeleteDocumentAsync(CreateDocumentUri(id),
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});
                }
                catch (DocumentClientException dcex)
                {
                    _logger.DocumentClientException(nameof(RemoveAsync), (int?) dcex.StatusCode ?? -1, dcex.Error,
                        dcex.Message);
                    throw new DocumentDbStateSessionExceptionException($"RemoveAsync for {id} failed, {dcex.Message}",
                        dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"RemoveAsync for {id} failed, {ex.Message}", ex);
                }
            }

            public async Task EnqueueAsync<T>(string schema, T value, IValueMetadata metadata,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var stateQueueInfo = await GetOrAddQueueInfo(schema);
                try
                {
                    var head = stateQueueInfo.HeadKey;
                    head++;
                    stateQueueInfo.HeadKey = head;

                    var key = head.ToString();
                    var id = _managerInternals.GetKey(new QueueItemStateKey(schema, head));
                    var document = _managerInternals.BuildWrapperGeneric(metadata, id, value);
                    await _documentClient.UpsertDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                        document,
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});
                    await SetQueueInfo(schema, stateQueueInfo);
                }
                catch (DocumentClientException dcex)
                {
                    _logger.DocumentClientException(nameof(EnqueueAsync), (int?) dcex.StatusCode ?? -1, dcex.Error,
                        dcex.Message);
                    throw new DocumentDbStateSessionExceptionException(
                        $"EnqueueAsync for {schema} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"EnqueueAsync for {schema} failed, {ex.Message}", ex);
                }
            }

            public async Task<ConditionalValue<T>> DequeueAsync<T>(string schema,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var stateQueueInfo = await GetOrAddQueueInfo(schema);
                try
                {
                    var tail = stateQueueInfo.TailKey;
                    var head = stateQueueInfo.HeadKey;

                    if (tail == head)
                        return new ConditionalValue<T>(false, default(T));

                    var id = _managerInternals.GetKey(new QueueItemStateKey(schema, tail)).GetId();

                    var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(
                        CreateDocumentUri(DatabaseName),
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionId)});
                    await _documentClient.DeleteDocumentAsync(CreateDocumentUri(id),
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

                    tail++;
                    stateQueueInfo.TailKey = tail;
                    await SetQueueInfo(schema, stateQueueInfo);

                    return new ConditionalValue<T>(true, response.Document.State);
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        return new ConditionalValue<T>(false, default(T));
                    throw new DocumentDbStateSessionExceptionException(
                        $"DequeueAsync for {schema} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"DequeueAsync for {schema} failed, {ex.Message}", ex);
                }
            }

            public async Task<ConditionalValue<T>> PeekAsync<T>(string schema,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var stateQueueInfo = await GetOrAddQueueInfo(schema);
                try
                {
                    var tail = stateQueueInfo.TailKey;
                    var head = stateQueueInfo.HeadKey;

                    if (tail == head)
                        return new ConditionalValue<T>(false, default(T));

                    var id = _managerInternals.GetKey(new QueueItemStateKey(schema, tail)).GetId();

                    var response = await _documentClient.ReadDocumentAsync<StateWrapper<T>>(CreateDocumentUri(id),
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

                    return new ConditionalValue<T>(true, response.Document.State);
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        return new ConditionalValue<T>(false, default(T));
                    throw new DocumentDbStateSessionExceptionException($"PeekAsync for {schema} failed, {dcex.Message}",
                        dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"PeekAsync for {schema} failed, {ex.Message}", ex);
                }
            }

            public async Task<long> GetDictionaryCountAsync<T>(string schema, CancellationToken cancellationToken)
            {
                try
                {
                    var resultCount = await _documentClient.CreateDocumentQuery<StateWrapper>(
                            UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                            new FeedOptions
                            {
                                MaxDegreeOfParallelism = -1,
                                PartitionKey = new PartitionKey(ServicePartitionKey)
                            })
                        .CountAsync(cancellationToken);

                    return resultCount;
                }
                catch (DocumentClientException dcex)
                {
                    throw new DocumentDbStateSessionExceptionException(
                        $"GetDictionaryCountAsync for {schema} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"GetDictionaryCountAsync for {schema} failed, {ex.Message}", ex);
                }
            }

            public async Task<long> GetEnqueuedCountAsync<T>(string schema, CancellationToken cancellationToken)
            {
                var stateQueueInfo = await GetOrAddQueueInfo(schema);
                try
                {
                    var head = stateQueueInfo.HeadKey;
                    var tail = stateQueueInfo.TailKey;

                    return head - tail;
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"GetEnqueuedCountAsync for {schema} failed, {ex.Message}", ex);
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

            public Task<bool> Contains<T>(string schema, string key,
                CancellationToken cancellationToken = new CancellationToken())
            {
                return Contains(schema, key, cancellationToken);
            }

            public Task<bool> Contains(string schema, string key,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var id = _managerInternals.GetKey(new DictionaryStateKey(schema, key)).GetId();
                try
                {
                    var docExists = _documentClient.CreateDocumentQuery(
                            UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                            new FeedOptions
                            {
                                PartitionKey = new PartitionKey(ServicePartitionKey),
                                MaxItemCount = 1
                            })
                        .Where(d => d.Id == id);

                    foreach (var document in docExists)
                        return Task.FromResult(true);
                    return Task.FromResult(false);
                }
                catch (DocumentClientException dcex)
                {
                    if (dcex.StatusCode == HttpStatusCode.NotFound)
                        return Task.FromResult(false);
                    throw new DocumentDbStateSessionExceptionException($"Contains for {id} failed, {dcex.Message}",
                        dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"Contains for {id} failed, {ex.Message}", ex);
                }
            }

            public Task<FindByKeyPrefixResult<T>> FindByKeyPrefixAsync<T>(string schema, string keyPrefix,
                int maxNumResults = 100000,
                ContinuationToken continuationToken = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }

            public async Task<FindByKeyPrefixResult> FindByKeyPrefixAsync(string schema, string keyPrefix,
                int maxNumResults = 100000,
                ContinuationToken continuationToken = null,
                CancellationToken cancellationToken = new CancellationToken())
            {
                var results = new List<string>();
                var resultCount = 0;
                var idPrefix = _managerInternals.GetKey(new DictionaryStateKey(schema, keyPrefix)).GetId();
                try
                {
                    IDocumentQuery<IdWrapper> documentCollectionQuery;
                    if (continuationToken?.Marker is string nextToken)
                        documentCollectionQuery = _documentClient.CreateDocumentQuery<IdWrapper>(
                                UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                                new FeedOptions
                                {
                                    PartitionKey = new PartitionKey(ServicePartitionKey),
                                    MaxItemCount = maxNumResults,
                                    RequestContinuation = nextToken
                                })
                            .Where(d => d.ServiceName.Equals(idPrefix))
                            .AsDocumentQuery();
                    else
                        documentCollectionQuery = _documentClient.CreateDocumentQuery<IdWrapper>(
                                UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                                new FeedOptions
                                {
                                    PartitionKey = new PartitionKey(ServicePartitionKey),
                                    MaxItemCount = maxNumResults
                                })
                            .Where(d => d.ServiceName.Equals(idPrefix))
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

                            if (resultCount > maxNumResults)
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
                    throw new DocumentDbStateSessionExceptionException(
                        $"FindByKeyPrefixAsync for {idPrefix} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"FindByKeyPrefixAsync for {idPrefix} failed, {ex.Message}", ex);
                }
            }

            public async Task<IEnumerable<string>> EnumerateSchemaNamesAsync(string key,
                CancellationToken cancellationToken = new CancellationToken())
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
                        var response =
                            await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

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
                    throw new DocumentDbStateSessionExceptionException(
                        $"EnumerateSchemaNamesAsync for {key} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"EnumerateSchemaNamesAsync for {key} failed, {ex.Message}", ex);
                }
            }

            private void AttachObjects(IEnumerable<IStateSessionReadOnlyObject> stateSessionObjects)
            {
                _attachedObjects = stateSessionObjects;
                foreach (var stateSessionObject in _attachedObjects)
                {
                    if (!(stateSessionObject is StateSessionBaseObject<DocumentDbStateSession> stateSessionBaseObject))
                        throw new StateSessionException(
                            $"Can only attach object that have been created by the owning StateSessionManager");
                    stateSessionBaseObject.AttachToSession(this);
                }
            }

            private void DetachObjects()
            {
                foreach (var stateSessionObject in _attachedObjects)
                {
                    if (!(stateSessionObject is StateSessionBaseObject<DocumentDbStateSession> stateSessionBaseObject))
                        throw new StateSessionException(
                            $"Can only detach object that have been created by the owning StateSessionManager");
                    stateSessionBaseObject.DetachFromSession(this);
                }
                _attachedObjects = new IStateSessionObject[0];
            }

            private Uri CreateDocumentUri(string schemaKey)
            {
                return UriFactory.CreateDocumentUri(DatabaseName, DatabaseCollection, schemaKey);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                    DetachObjects();
            }

            private async Task<QueueInfo> GetOrAddQueueInfo(string schema)
            {
                var stateKeyQueueInfo = _managerInternals.GetKey(new QueueInfoStateKey(schema)).GetId();
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
                        var value = new QueueInfo
                        {
                            HeadKey = 0L,
                            TailKey = 0L
                        };

                        return await SetQueueInfo(schema, value);
                    }
                    throw new DocumentDbStateSessionExceptionException(
                        $"DequeueAsync for {stateKeyQueueInfo} failed, {dcex.Message}", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"EnqueueAsync for {stateKeyQueueInfo} failed, {ex.Message}", ex);
                }
            }

            private async Task<QueueInfo> SetQueueInfo(string schema, QueueInfo value)
            {
                var id = _managerInternals.GetKey(new QueueInfoStateKey(schema));
                try
                {
                    var metadata = new ValueMetadata(StateWrapperType.ReliableQueueItem);
                    var document = _managerInternals.BuildWrapperGeneric(metadata, id, value);
                    await _documentClient.UpsertDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(DatabaseName, DatabaseCollection),
                        document,
                        new RequestOptions {PartitionKey = new PartitionKey(ServicePartitionKey)});

                    return document.State;
                }
                catch (DocumentClientException dcex)
                {
                    throw new DocumentDbStateSessionExceptionException($"CreateQueueInfo for {id} failed", dcex);
                }
                catch (Exception ex)
                {
                    throw new StateSessionException($"CreateQueueInfo for {id} failed", ex);
                }
            }
        }

        public class DocumentDbStateQuerySession : IStateQuerySession
        {
            private readonly DocumentClient _documentClient;
            private readonly DocumentDbStateSessionManager _manager;

            public DocumentDbStateQuerySession(
                DocumentDbStateSessionManager manager)
            {
                _manager = manager;
                _documentClient = _manager._factory.OpenAsync( // TODO: Do proper init.
                    _manager._databaseName,
                    new CosmosDbCollectionDefinition(_manager._collection, $"/partitionKey"),
                    new Uri(_manager._endpointUri),
                    _manager._collectionPrimaryKey,
                    _manager._connectionPolicySetting).GetAwaiter().GetResult();
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
                        var response =
                            await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

                        foreach (var documentId in response)
                        {
                            resultCount++;
                            if (!results.Contains(documentId.ServiceName))
                                results.Add(documentId.ServiceName);
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
                        var response =
                            await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

                        foreach (var documentId in response)
                        {
                            resultCount++;
                            if (!results.Contains(documentId.PartitionKey))
                                results.Add(documentId.PartitionKey);
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
                        var response =
                            await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

                        foreach (var documentId in response)
                        {
                            resultCount++;
                            var schemaKey = $"{documentId.Schema}_{documentId.Key}";
                            if (!results.Contains(schemaKey))
                                results.Add(schemaKey);
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
                        var response =
                            await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

                        foreach (var documentId in response)
                        {
                            resultCount++;
                            if (!results.Contains(documentId.Key))
                                results.Add(documentId.Key);
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
                        var response =
                            await documentCollectionQuery.ExecuteNextAsync<StateWrapper>(CancellationToken.None);

                        foreach (var documentId in response)
                        {
                            resultCount++;
                            if (!results.Contains(documentId.Key))
                                results.Add(documentId.Key);
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


            private Uri CreateDocumentUri(string schemaKey)
            {
                return UriFactory.CreateDocumentUri(DatabaseName, DatabaseCollection, schemaKey);
            }
        }
    }
}