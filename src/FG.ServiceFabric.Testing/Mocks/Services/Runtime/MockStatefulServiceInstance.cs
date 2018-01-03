using System.Fabric;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Data;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using Microsoft.ServiceFabric.Data;
using StatefulService = Microsoft.ServiceFabric.Services.Runtime.StatefulService;

namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    internal class MockStatefulServiceInstance : MockServiceInstance
    {
        public IReliableStateManagerReplica2 StateManager { get; private set; }

        private StatefulService GetMockStatefulService(
            StatefulServiceContext serviceContext,
            IReliableStateManagerReplica2 stateManager)
        {
            return new MockStatefulService(
                serviceContext.CodePackageActivationContext,
                FabricRuntime.ServiceProxyFactory,
                FabricRuntime.BuildNodeContext(),
                serviceContext,
                stateManager);
        }

        protected override void Build()
        {
            var isStatefull = ServiceRegistration?.IsStateful ?? false;

            if (!isStatefull)
            {
                base.Build();
                return;
            }

            var statefulServiceContext = FabricRuntime.BuildStatefulServiceContext(
                ServiceRegistration.GetApplicationName(),
                ServiceRegistration.Name,
                Partition.PartitionInformation,
                Replica.Id,
                ServiceManifest,
                ServiceConfig);
            StateManager = (ServiceRegistration.CreateStateManager ??
                            (() => (IReliableStateManagerReplica2) new MockReliableStateManager(FabricRuntime)))
                .Invoke();
            var serviceFactory = ServiceRegistration.CreateStatefulService ?? GetMockStatefulService;
            // TODO: consider this further, is it really what should be done???

            var statefulService = serviceFactory(statefulServiceContext, StateManager);
            if (statefulService is ServiceFabric.Services.Runtime.StatefulService)
            {
                var applicationUriBuilder = new ApplicationUriBuilder(
                    statefulServiceContext.CodePackageActivationContext,
                    statefulServiceContext.CodePackageActivationContext.ApplicationName);
                statefulService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
                statefulService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
                statefulService.SetPrivateField("_applicationUriBuilder", applicationUriBuilder);
            }

            ServiceInstance = statefulService;

            base.Build();
        }
    }
}