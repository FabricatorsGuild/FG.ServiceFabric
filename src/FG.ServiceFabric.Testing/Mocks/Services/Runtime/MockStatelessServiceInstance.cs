using System.Fabric;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
	internal class MockStatelessServiceInstance : MockServiceInstance
	{
		private StatelessService GetMockStatelessService(
			StatelessServiceContext serviceContext)
		{
			return new MockStatelessService(
				codePackageActivationContext: FabricRuntime.CodePackageContext,
				serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
				nodeContext: FabricRuntime.BuildNodeContext(),
				statelessServiceContext: serviceContext);
		}

		protected override void Build()
		{
			var isStateless = (!ServiceRegistration?.IsStateful) ?? false;

			if (!isStateless)
			{
				base.Build();
				return;
			}


			var statelessServiceContext = FabricRuntime.BuildStatelessServiceContext(ServiceRegistration.Name);
			var serviceFactory = ServiceRegistration.CreateStatelessService ?? GetMockStatelessService;
			// TODO: consider this further, is it really what should be done???

			var statelessService = serviceFactory(statelessServiceContext);
			if (statelessService is FG.ServiceFabric.Services.Runtime.StatelessService)
			{
				statelessService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
				statelessService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
				statelessService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
			}

			ServiceInstance = statelessService;

			base.Build();
		}
	}
}