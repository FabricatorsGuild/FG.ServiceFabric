using System;
using System.Fabric;
using System.Fabric.Query;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Fabric
{
    public interface IPartitionEnumerationManager
    {
        Task<ServicePartitionList> GetPartitionListAsync(Uri serviceName);

	    Task<ServicePartitionInformation> GetPartition(Uri serviceName, long partitionKey);
		Task<ServicePartitionInformation> GetPartition(Uri serviceName, string partitionKey);
	}
}