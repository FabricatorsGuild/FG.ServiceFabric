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
        Task<DocumentClient> OpenAsync(string databaseName, CosmosDbCollectionDefinition collection, Uri endpointUri, string primaryKey, ConnectionPolicySetting connectionPolicySetting);
    }

	public class CosmosDbCollectionDefinition
	{
		public CosmosDbCollectionDefinition(string collectionName, params string[] partitionKeyPaths)
		{
			CollectionName = collectionName;
			PartitionKeyPaths = partitionKeyPaths;
		}

		public string CollectionName { get; set; }
		public string[] PartitionKeyPaths { get; set; }
	}

    public class CosmosDbClientFactory : ICosmosDbClientFactory
    {
		

        public async Task<DocumentClient> OpenAsync(
			string databaseName,
			CosmosDbCollectionDefinition collection, 
			Uri endpointUri, 
			string primaryKey, 
			ConnectionPolicySetting connectionPolicySetting = ConnectionPolicySetting.GatewayHttps)
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
        public static async Task EnsureStoreIsConfigured(this IDocumentClient @this, string databaseName, CosmosDbCollectionDefinition collection)
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

        public static async Task CreateCollection(this IDocumentClient @this, Resource store, CosmosDbCollectionDefinition collection)
        {
            var readDocumentCollectionFeedAsync = await @this.ReadDocumentCollectionFeedAsync(store.SelfLink);
            var documentCollection = readDocumentCollectionFeedAsync.FirstOrDefault(x => x.Id == collection.CollectionName);

            if (documentCollection == null)
            {
	            var partitionKeyDefinition = new PartitionKeyDefinition();
	            foreach (var partitionKeyPath in collection.PartitionKeyPaths)
	            {
		            partitionKeyDefinition.Paths.Add(partitionKeyPath);
	            }

				var collectionSpec = new DocumentCollection
                {
                    Id = collection.CollectionName,
					PartitionKey = partitionKeyDefinition,
                };

                await @this.CreateDocumentCollectionAsync(store.SelfLink, collectionSpec);
            }
        }
    }
}