using System.Fabric;
using FG.ServiceFabric.Utils;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
    public class DbStoredActorSettingsProvider : SettingsProviderBase
    {
        public DbStoredActorSettingsProvider(ServiceContext context) : base(context)
        {
            Configure()
                .FromSettings("Database", "Database_EndpointUri")
                .FromSettings("Database", "Database_PrimaryKey")
                .FromSettings("Database", "Database_Name")
                .FromSettings("Database", "Database_Collection");
        }
    }
}
