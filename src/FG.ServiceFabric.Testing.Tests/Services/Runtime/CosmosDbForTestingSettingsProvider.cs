using System.Collections.Generic;
using System.Linq;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Utils;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime.With_StateSessionManager.and_DocumentDbStateSessionManagerWithTransaction
{
	public class CosmosDbForTestingSettingsProvider : ISettingsProvider
	{
		private readonly IDictionary<string, string> _settings = new Dictionary<string, string>();

		public static CosmosDbForTestingSettingsProvider DefaultForCollection(string collection) => new CosmosDbForTestingSettingsProvider("https://ffcg-labs-docdb.documents.azure.com:443/", "sfp-local1",
			collection,
			"Rvh80izCLQJJWOitTes652Qkh6UyLjEhZauCWXHFkY0H3nzdh3iQee7OI3qtO42jgIOLwH7q3j25Hk0onbAIWQ==");

		public CosmosDbForTestingSettingsProvider(string endpointUri, string databaseName, string collection, string primaryKey)
		{
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyEndpointUri}", endpointUri);
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyDatabaseName}", databaseName);
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}", collection);
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyPrimaryKey}", primaryKey);
		}

		public bool Contains(string key)
		{
			return _settings.ContainsKey(key);
		}

		public string this[string key] => _settings[key];

		public string[] Keys => _settings.Keys.ToArray();

		public void AppendCollectionName(string appended)
		{
			_settings[$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}"] =
				$"dev-sfp-testing-{appended}";
		}

		public string CollectionName =>
			_settings[$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}"];
	}
}