using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

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

			var documentClient = new DocumentClient(endpointUri, primaryKey, connectionPolicy);
			await documentClient.OpenAsync();
			return documentClient;
		}
	}
}