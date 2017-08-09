using System.Fabric;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks.Data;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
	internal class MockStatefulServiceInstance : MockServiceInstance
	{
		public IReliableStateManagerReplica StateManager { get; private set; }

		private StatefulService GetMockStatefulService(
			StatefulServiceContext serviceContext,
			IReliableStateManagerReplica stateManager)
		{
			return new MockStatefulService(
				codePackageActivationContext: FabricRuntime.CodePackageContext,
				serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
				nodeContext: FabricRuntime.BuildNodeContext(),
				statefulServiceContext: serviceContext,
				stateManager: stateManager);
		}

		protected override void Build()
		{
			var isStatefull = ServiceRegistration?.IsStateful ?? false;

			if (!isStatefull)
			{
				base.Build();
				return;
			}

			var statefulServiceContext = FabricRuntime.BuildStatefulServiceContext(ServiceRegistration.Name);
			StateManager = (ServiceRegistration.CreateStateManager ??
			                (() => (IReliableStateManagerReplica)new MockReliableStateManager(FabricRuntime))).Invoke();
			var serviceFactory = ServiceRegistration.CreateStatefulService ?? GetMockStatefulService;
			// TODO: consider this further, is it really what should be done???

			var statefulService = serviceFactory(statefulServiceContext, StateManager);
			if (statefulService is FG.ServiceFabric.Services.Runtime.StatefulService)
			{
				statefulService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
				statefulService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
				statefulService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
			}

			ServiceInstance = statefulService;

			base.Build();
		}		
	}
}