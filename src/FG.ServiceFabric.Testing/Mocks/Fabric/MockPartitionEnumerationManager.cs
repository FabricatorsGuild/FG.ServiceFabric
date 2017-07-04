using System;
using System.Collections.Generic;
using System.Fabric.Query;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Fabric;

namespace FG.ServiceFabric.Testing.Mocks.Fabric
{
    public class MockPartitionEnumerationManager : IPartitionEnumerationManager
    {
	    private ServicePartitionList _servicePartitionList;

		public MockPartitionEnumerationManager(params Partition[] partitions)
	    {
			var servicePartitionListType = typeof(ServicePartitionList);
			var servicePartitionList = ReflectionUtils.CreateInstanceOfInternal(servicePartitionListType, new List<Partition>(partitions)) as ServicePartitionList;
			_servicePartitionList = servicePartitionList;
		}

		public Task<ServicePartitionList> GetPartitionListAsync(Uri serviceName)
        {
            return Task.FromResult(_servicePartitionList);
        }
    }
}