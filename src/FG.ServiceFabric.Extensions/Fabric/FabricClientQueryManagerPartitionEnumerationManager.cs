using System;
using System.Fabric;
using System.Fabric.Query;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Fabric
{
    public class FabricClientQueryManagerPartitionEnumerationManager : IPartitionEnumerationManager
    {
        private readonly FabricClient.QueryClient _fabricClientQueryManager;

        public FabricClientQueryManagerPartitionEnumerationManager(FabricClient.QueryClient queryManager)
        {
            _fabricClientQueryManager = queryManager;
        }

        public Task<ServicePartitionList> GetPartitionListAsync(Uri serviceName)
        {
            return _fabricClientQueryManager.GetPartitionListAsync(serviceName);
        }
    }
}