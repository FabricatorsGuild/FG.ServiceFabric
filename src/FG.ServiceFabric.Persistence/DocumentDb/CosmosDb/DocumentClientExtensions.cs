using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
	internal static class DocumentClientExtensions
	{
		public static async Task EnsureStoreIsConfigured(this IDocumentClient @this, string databaseName,
			CosmosDbCollectionDefinition collection)
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

		public static async Task CreateCollection(this IDocumentClient @this, Resource store,
			CosmosDbCollectionDefinition collection)
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

		public static async Task DestroyCollection(this IDocumentClient @this, string databaseId, string collectionId)
		{
			var readDocumentCollectionFeedAsync = await @this.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId));
		}
	}
}