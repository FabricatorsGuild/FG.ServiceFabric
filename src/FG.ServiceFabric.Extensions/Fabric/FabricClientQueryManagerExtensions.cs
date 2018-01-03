using System.Fabric;

namespace FG.ServiceFabric.Fabric
{
    public static class FabricClientQueryManagerExtensions
    {
        public static IPartitionEnumerationManager GetPartitionEnumerationManager(this FabricClient fabricClient)
        {
            return new FabricClientQueryManagerPartitionEnumerationManager(fabricClient);
        }
    }
}