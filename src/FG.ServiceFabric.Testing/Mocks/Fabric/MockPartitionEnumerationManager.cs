using System;
using System.Collections.Generic;
using System.Fabric.Query;
using System.Linq;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Fabric;

namespace FG.ServiceFabric.Testing.Mocks.Fabric
{
    public class MockPartitionEnumerationManager : IPartitionEnumerationManager
    {
	    private readonly MockFabricRuntime _fabricRuntime;

	    public MockPartitionEnumerationManager(MockFabricRuntime fabricRuntime)
	    {
		    _fabricRuntime = fabricRuntime;
	    }
		
		public Task<ServicePartitionList> GetPartitionListAsync(Uri serviceName)
		{
			var instances = _fabricRuntime.Instances.Where(i => i.ServiceUri == serviceName);
			if (!instances.Any())
			{
				throw new NotSupportedException($"Cannot enumerate partitions for {serviceName}, call SetupService on MockFabricRuntime first");
			}
			var partitions = _fabricRuntime.Instances.Where(i => i.ServiceUri == serviceName).Select(i => i.Partition);

			var servicePartitionListType = typeof(ServicePartitionList);
			var servicePartitionList = ReflectionUtils.CreateInstanceOfInternal(servicePartitionListType, new List<Partition>(partitions)) as ServicePartitionList;
			
			return Task.FromResult(servicePartitionList);
        }
    }
}