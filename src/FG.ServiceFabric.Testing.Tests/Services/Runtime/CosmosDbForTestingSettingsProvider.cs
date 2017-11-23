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
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyEndpointUri}", "");
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyDatabaseName}", "");
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}", "");
			_settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyPrimaryKey}", "");
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
	}
}