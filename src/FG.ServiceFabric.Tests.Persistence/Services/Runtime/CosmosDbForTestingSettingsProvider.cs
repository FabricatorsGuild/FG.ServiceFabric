using System.Collections.Generic;
using System.Linq;
using FG.Common.Settings;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using FG.ServiceFabric.Utils;

namespace FG.ServiceFabric.Tests.Persistence.Services.
    Runtime
{
    public class CosmosDbForTestingSettingsProvider : ISettingsProvider
    {
        private readonly IDictionary<string, string> _settings = new Dictionary<string, string>();

        public CosmosDbForTestingSettingsProvider(string endpointUri, string databaseName, string collection,
            string primaryKey)
        {
            _settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyEndpointUri}",
                endpointUri);
            _settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyDatabaseName}",
                databaseName);
            _settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}",
                collection);
            _settings.Add($"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyPrimaryKey}",
                primaryKey);
        }

        public string CollectionName =>
            _settings[$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}"];

        public bool Contains(string key)
        {
            return _settings.ContainsKey(key);
        }

        public string this[string key] => _settings[key];

        public string[] Keys => _settings.Keys.ToArray();

        public static CosmosDbForTestingSettingsProvider DefaultForCollection(string collection)
        {
            return new CosmosDbForTestingSettingsProvider("https://ffcg-labs-docdb.documents.azure.com:443/",
                "sfp-local1",
                collection,
                "Gqc9vu8Zm9vOFIcGfOyrjEZThdpMHtv8Ys4Rji22IQtMVS9d7iwzGvvU6DxNggk10TiHQ72Fo2TxRinbJSaFCw==");
        }

        public void AppendCollectionName(string appended)
        {
            _settings[$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}"] =
                $"dev-sfp-testing-{appended}";
        }
    }
}