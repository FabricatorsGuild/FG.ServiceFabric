using System.Fabric;
using FG.ServiceFabric.Services.Remoting.FabricTransport;

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