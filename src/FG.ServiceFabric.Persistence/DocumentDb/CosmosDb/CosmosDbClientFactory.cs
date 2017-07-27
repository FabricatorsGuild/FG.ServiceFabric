using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
    public enum ConnectionPolicySetting
    {
        None = 0,
        DirectTcp = 1,
        GatewayHttps = 2
    }

    public interface ICosmosDbClientFactory
    {
        Task<DocumentClient> OpenAsync(string databaseName, string collection, Uri endpointUri, string primaryKey, ConnectionPolicySetting connectionPolicySetting);
    }
    
    public class CosmosDbClientFactory : ICosmosDbClientFactory
    {
        public async Task<DocumentClient> OpenAsync(string databaseName, string collection, Uri endpointUri, string primaryKey, ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps)
        {
            ConnectionPolicy connectionPolicy;
            switch (connectionPolicySetting)
            {
                case ConnectionPolicySetting.None:
                    connectionPolicy = null;
                    break;
                case ConnectionPolicySetting.DirectTcp:
                    connectionPolicy = new ConnectionPolicy
                    {
                        ConnectionMode = ConnectionMode.Direct,
                        ConnectionProtocol = Protocol.Tcp
                    };
                    break;
                case ConnectionPolicySetting.GatewayHttps:
                    connectionPolicy = new ConnectionPolicy
                    {
                        ConnectionMode = ConnectionMode.Gateway,
                        ConnectionProtocol = Protocol.Https
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(connectionPolicySetting), connectionPolicySetting, null);
            }
            
            var documentClient = new DocumentClient(endpointUri, primaryKey, connectionPolicy);
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
            {
                store = await @this.CreateDatabaseAsync(new Database {Id = databaseName});
            }
            else
            {
                store = currentDatabases.FirstOrDefault(x => x.Id == databaseName);
            }

            if (store != null)
            {
                await @this.CreateCollection(store, collection);
            }   
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