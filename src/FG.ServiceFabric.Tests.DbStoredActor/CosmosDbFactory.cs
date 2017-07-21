using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.Utils;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
    public enum ConnectionPolicySetting
    {
        None = 0, DirectTcp, GatewayHttps
    }

    public interface ICosmosDocumentClientFactory
    {
        Task<DocumentClient> Create(string databaseName, string collection, ISettingsProvider settingsProvider, ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps);
    }
    
    public class CosmosDocumentClientFactory : ICosmosDocumentClientFactory
    {
        private string _databaseName;
        private string _collection;

        public async Task<DocumentClient> Create(string databaseName, string collection, ISettingsProvider settingsProvider, ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps)
        {
            var endpointUri = settingsProvider["Database_EndpointUri"];
            var primaryKey = settingsProvider["Database_PrimaryKey"];

            _databaseName = databaseName;
            _collection = collection;

            ConnectionPolicy connectionPolicy;
            if (connectionPolicySetting == ConnectionPolicySetting.DirectTcp)
            {
                connectionPolicy = new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                };
            }
            else
            {
                connectionPolicy = new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    ConnectionProtocol = Protocol.Https
                };
            }

            var documentClient = new DocumentClient(new Uri(endpointUri), primaryKey, connectionPolicy);
            await documentClient.OpenAsync();
            await documentClient.EnsureStoreIsConfigured(databaseName, collection);
            return documentClient;
        }
    }

    internal static class DocumentClientExtensions
    {
        public static async Task EnsureStoreIsConfigured(this IDocumentClient @this, string databaseName, string collection)
        {
            var currentDatabases = @this.CreateDatabaseQuery().AsEnumerable().ToList();

            Database store;

            if (currentDatabases.FirstOrDefault(x => x.Id == databaseName) == null)
                store = await @this.CreateDatabaseAsync(new Database { Id = databaseName });
            else
                store = currentDatabases.FirstOrDefault(x => x.Id == databaseName);

            if (store != null)
                await @this.CreateCollection(store, collection);
        }

        public static async Task CreateCollection(this IDocumentClient @this, Resource store, string collection)
        {
            var readDocumentCollectionFeedAsync = await @this.ReadDocumentCollectionFeedAsync(store.SelfLink);
            var documentCollection = readDocumentCollectionFeedAsync.FirstOrDefault(x => x.Id == collection);

            if (documentCollection == null)
            {
                var collectionSpec = new DocumentCollection
                {
                    Id = collection
                };

                await @this.CreateDocumentCollectionAsync(store.SelfLink, collectionSpec);
            }
        }
    }
}