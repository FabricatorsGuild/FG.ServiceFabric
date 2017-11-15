using System;
using System.Fabric;
using System.Linq;
using FG.ServiceFabric.Utils;
using FG.Common.Utils;

namespace FG.ServiceFabric.DocumentDb.CosmosDb
{
	public class CosmosDbSettingsProvider : SettingsProviderBase
	{
		public const string ConfigSection = @"CosmosDb";
		public const string ConfigKeyEndpointUri = @"EndpointUri";
		public const string ConfigKeyPrimaryKey = @"PrimaryKey";
		public const string ConfigKeyDatabaseName = @"DatabaseName";
		public const string ConfigKeyCollection = @"Collection";

		public CosmosDbSettingsProvider(ServiceContext context) : base(context)
		{
			Configure()
				.FromSettings(ConfigSection, ConfigKeyEndpointUri, RegistrationBuilder.KeyNameBuilder.SectionAndKeyName)
				.FromSettings(ConfigSection, ConfigKeyPrimaryKey, RegistrationBuilder.KeyNameBuilder.SectionAndKeyName)
				.FromSettings(ConfigSection, ConfigKeyDatabaseName, RegistrationBuilder.KeyNameBuilder.SectionAndKeyName)
				.FromSettings(ConfigSection, ConfigKeyCollection, RegistrationBuilder.KeyNameBuilder.SectionAndKeyName);
		}
	}

	public static class CosmosDbSettingsProviderExtensions
	{
		private static string GetByKey(ISettingsProvider settingsProvider, string key)
		{
			if (!settingsProvider.Contains(key))
			{
				throw new ArgumentException(
					$"The key {key} did not exist in the settingsprovider. Provider contains {settingsProvider.Keys.Length} keys: {settingsProvider.Keys.Concat(", ")}");
			}
			return settingsProvider[key];
		}

		public static string EndpointUri(this ISettingsProvider settingsProvider)
		{
			return settingsProvider[$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyEndpointUri}"];
		}

		public static string PrimaryKey(this ISettingsProvider settingsProvider)
		{
			var key = $"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyPrimaryKey}";

			return settingsProvider[$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyPrimaryKey}"];
		}

		public static string DatabaseName(this ISettingsProvider settingsProvider)
		{
			return settingsProvider
				[$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyDatabaseName}"];
		}

		public static string Collection(this ISettingsProvider settingsProvider)
		{
			return settingsProvider[$"{CosmosDbSettingsProvider.ConfigSection}.{CosmosDbSettingsProvider.ConfigKeyCollection}"];
		}
	}
}