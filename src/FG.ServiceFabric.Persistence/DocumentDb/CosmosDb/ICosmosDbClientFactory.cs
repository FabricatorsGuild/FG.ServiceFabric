using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
	public interface ICosmosDbClientFactory
	{
		Task<DocumentClient> OpenAsync(
			string databaseName, 
			CosmosDbCollectionDefinition collection, 
			Uri endpointUri,
			string primaryKey, 
			ConnectionPolicySetting connectionPolicySetting);
	}
}