namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
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
}