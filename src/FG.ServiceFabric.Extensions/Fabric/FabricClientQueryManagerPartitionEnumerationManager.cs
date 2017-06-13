using System;
using System.Fabric;
using System.Fabric.Query;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Fabric
{
    public class FabricClientQueryManagerPartitionEnumerationManager : IPartitionEnumerationManager
    {
        private readonly FabricClient _fabricClient;
        public FabricClientQueryManagerPartitionEnumerationManager(FabricClient fabricClient)
        {
            _fabricClient = fabricClient;
        }

        public Task<ServicePartitionList> GetPartitionListAsync(Uri serviceName)
        {
            return _fabricClient.QueryManager.GetPartitionListAsync(serviceName);
        }
    }
}