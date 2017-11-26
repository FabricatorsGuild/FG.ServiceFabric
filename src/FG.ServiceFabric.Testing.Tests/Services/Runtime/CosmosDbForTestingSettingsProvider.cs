using System.Collections.Generic;
using System.Linq;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Utils;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime.With_StateSessionManager.and_DocumentDbStateSessionManagerWithTransaction
{
	public class CosmosDbForTestingSettingsProvider : ISettingsProvider
	{
		private readonly IDictionary<string, string> _settings = new Dictionary<string, string>();

		public CosmosDbForTestingSettingsProvider()
		{
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyEndpointUri}", "https://172.27.95.75:8081");
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyDatabaseName}", "sfp");
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}", "tests");
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyPrimaryKey}", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
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