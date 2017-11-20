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

		public async Task<ServicePartitionInformation> GetPartition(Uri serviceName, long partitionKey)
		{
			var resolvedPartition = await _fabricClient.ServiceManager.ResolveServicePartitionAsync(serviceName, partitionKey);
			return resolvedPartition.Info;
		}

		public async Task<ServicePartitionInformation> GetPartition(Uri serviceName, string partitionKey)
		{
			var resolvedPartition = await _fabricClient.ServiceManager.ResolveServicePartitionAsync(serviceName, partitionKey);
			return resolvedPartition.Info;
		}
	}
}