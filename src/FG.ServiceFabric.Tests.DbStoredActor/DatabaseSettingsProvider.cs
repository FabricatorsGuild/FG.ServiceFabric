using System.Fabric;
using FG.ServiceFabric.Utils;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
	public class DatabaseSettingsProvider : SettingsProviderBase
	{
		public DatabaseSettingsProvider(ServiceContext context) : base(context)
		{
			Configure()
				.FromSettings("Database", "EndpointUri")
				.FromSettings("Database", "PrimaryKey")
				.FromSettings("Database", "DatabaseName")
				.FromSettings("Database", "Collection");
		}
	}
}