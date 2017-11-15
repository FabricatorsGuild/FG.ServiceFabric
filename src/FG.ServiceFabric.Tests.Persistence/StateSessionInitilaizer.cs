using System;
using System.Fabric;
using FG.ServiceFabric.Fabric;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Testing.Mocks;

namespace FG.ServiceFabric.Tests.Persistence
{
	public class StateSessionInitilaizer
	{
		public static IStateSessionManager CreateStateManager(ServiceContext context)
		{
			var partitionEnumerationManager = MockFabricRuntime.Current != null
				? (Func<IPartitionEnumerationManager>) (() =>
					(IPartitionEnumerationManager) MockFabricRuntime.Current.PartitionEnumerationManager)
				: (Func<IPartitionEnumerationManager>) (() =>
					(IPartitionEnumerationManager) new FabricClientQueryManagerPartitionEnumerationManager(new FabricClient()));
			return new InMemoryStateSessionManager(StateSessionHelper.GetServiceName(context.ServiceName), context.PartitionId,
				StateSessionHelper.GetPartitionInfo(context, partitionEnumerationManager).GetAwaiter().GetResult());
		}
	}
}