using System;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
	internal static class DocumentClientExtensions
	{
		public static async Task EnsureStoreIsConfigured(this IDocumentClient @this, 
			string databaseName,
			CosmosDbCollectionDefinition collection,
			IDocumentDbStateSessionManagerLogger logger)
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
				await @this.CreateCollection(store, collection, logger);
			}
		}

		public static async Task CreateCollection(
			this IDocumentClient @this, 
			Resource store,
			CosmosDbCollectionDefinition collection,
			IDocumentDbStateSessionManagerLogger logger)
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
				try
				{
					logger.CreatingCollection(collection.CollectionName);
					await @this.CreateDocumentCollectionAsync(store.SelfLink, collectionSpec);
				}
				catch (DocumentClientException ex)
				{
					if (ex.Error.Code == "Conflict")
					{
						return;						
					}
					throw;
				}
			}
		}

		public static async Task DestroyCollection(this IDocumentClient @this, string databaseId, string collectionId)
		{
			try
			{
				Console.WriteLine($"Destroying collection {collectionId} in database {databaseId}");
				var readDocumentCollectionFeedAsync = await @this.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId));
			}
			catch (DocumentClientException ex)
			{
				Console.WriteLine($"Failed to destroy collection {collectionId} in database {databaseId} - {ex.Message}");

				if (ex.Error.Code == "NotFound")
				{
					return;
				}
				throw;
			}
		}
	}
}