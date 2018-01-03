using System.Fabric;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using StatelessService = Microsoft.ServiceFabric.Services.Runtime.StatelessService;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    internal class MockStatelessServiceInstance : MockServiceInstance
    {
        private StatelessService GetMockStatelessService(
            StatelessServiceContext serviceContext)
        {
            return new MockStatelessService(
                serviceContext.CodePackageActivationContext,
                FabricRuntime.ServiceProxyFactory,
                FabricRuntime.BuildNodeContext(),
                serviceContext);
        }

        protected override void Build()
        {
            var isStateless = !ServiceRegistration?.IsStateful ?? false;

            if (!isStateless)
            {
                base.Build();
                return;
            }


            var statelessServiceContext = FabricRuntime.BuildStatelessServiceContext(
                ServiceRegistration.GetApplicationName(),
                ServiceRegistration.Name,
                ServiceManifest,
                ServiceConfig);
            var serviceFactory = ServiceRegistration.CreateStatelessService ?? GetMockStatelessService;
            // TODO: consider this further, is it really what should be done???

            var statelessService = serviceFactory(statelessServiceContext);
            if (statelessService is ServiceFabric.Services.Runtime.StatelessService)
            {
                var applicationUriBuilder = new ApplicationUriBuilder(
                    statelessServiceContext.CodePackageActivationContext,
                    statelessServiceContext.CodePackageActivationContext.ApplicationName);
                statelessService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
                statelessService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
                statelessService.SetPrivateField("_applicationUriBuilder", applicationUriBuilder);
            }

            ServiceInstance = statelessService;

            base.Build();
        }
    }
}