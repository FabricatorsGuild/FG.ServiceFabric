using System;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents.Client;

namespace FG.ServiceFabric.DocumentDb
{
    public class CosmosDbStateWriter : IDocumentDbStateWriter
    {
        private readonly string _databaseName;
        private readonly string _collection;
        private readonly ICosmosDbClientFactory _factory;
        private readonly ConnectionPolicySetting _connectionPolicySetting;
        private DocumentClient _documentClient = null;
        private readonly string _endpointiUri;
        private readonly string _primaryKey;

        public CosmosDbStateWriter(ISettingsProvider settingsProvider, ICosmosDbClientFactory factory = null, ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps)
        {
            _factory = factory ?? new CosmosDbClientFactory();

            _connectionPolicySetting = connectionPolicySetting;

            _collection = settingsProvider["Collection"];
            _databaseName = settingsProvider["DatabaseName"];
            _endpointiUri = settingsProvider["EndpointUri"];
            _primaryKey = settingsProvider["PrimaryKey"];
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
                _documentClient?.Dispose();
            }
        }
        
        public async Task UpsertAsync<T>(T state, string stateName) where T : IPersistedIdentity
        {
            try
            {
                await EnsureOpenAsync();

                var uri = UriFactory.CreateDocumentCollectionUri(_databaseName, _collection);
                var document = new DocumentStateWrapper<T> {Payload = state, StateName = stateName};

                var response = await _documentClient.UpsertDocumentAsync(uri, document);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        public async Task<IQueryable<T>> QueryAsync<T>() where T : IPersistedIdentity
        {
            try
            {
                await EnsureOpenAsync();

                var uri = UriFactory.CreateDocumentCollectionUri(_databaseName, _collection);
                return _documentClient.CreateDocumentQuery<DocumentStateWrapper<T>>(uri, new FeedOptions { MaxItemCount = 1 })
                    .Select(wrappedState => wrappedState.Payload);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task EnsureOpenAsync()
        {
            if (_documentClient == null)
            {
                _documentClient = await _factory.OpenAsync(_databaseName, _collection, new Uri(_endpointiUri), _primaryKey,
                    _connectionPolicySetting);
            }
        }
    }
}