using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
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

			var documentClient = new DocumentClient( 
				serviceEndpoint: endpointUri, 
				authKeyOrResourceToken: primaryKey, 
				connectionPolicy: connectionPolicy, 
				serializerSettings: new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }
				);
			await documentClient.OpenAsync();
			return documentClient;
		}
	}
}