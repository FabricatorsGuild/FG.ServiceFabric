using System;
using System.Collections;
using System.Fabric.Query;
using System.Threading.Tasks;

namespace FG.ServiceFabric.Fabric
{
    public interface IPartitionEnumerationManager
    {
        Task<ServicePartitionList> GetPartitionListAsync(Uri serviceName);
    }
}