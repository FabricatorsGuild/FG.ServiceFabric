using System;
using System.Fabric;
using System.Linq;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Testing.Setup;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Runtime;
using StatelessService = Microsoft.ServiceFabric.Services.Runtime.StatelessService;

namespace FG.ServiceFabric.Testing.Mocks
{
    public class MockFabricApplication
    {
        internal MockFabricApplication(MockFabricRuntime mockFabricRuntime, string applicationInstanceName)
        {
            FabricRuntime = mockFabricRuntime;
            ApplicationInstanceName = applicationInstanceName;
            ApplicationUriBuilder = FabricRuntime.GetApplicationUriBuilder(ApplicationInstanceName);
        }

        public MockFabricRuntime FabricRuntime { get; }

        public string ApplicationInstanceName { get; }

        public ApplicationUriBuilder ApplicationUriBuilder { get; }

        public void SetupService<TServiceImplementation>
        (
            Func<StatefulServiceContext, IReliableStateManagerReplica2, TServiceImplementation> createService,
            CreateStateManager createStateManager = null,
            MockServiceDefinition serviceDefinition = null,
            IServiceManifest serviceManifest = null,
            IServiceConfig serviceConfig = null
        )
            where TServiceImplementation : StatefulServiceBase
        {
            var serviceType = typeof(TServiceImplementation);
            var serviceInterfaceTypes = typeof(TServiceImplementation).IsInterface
                ? new[] {typeof(TServiceImplementation)}
                : typeof(TServiceImplementation)
                    .GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

            serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

            var serviceName = serviceType.Name;
            var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

            var serviceRegistration = new MockableServiceRegistration(
                serviceInterfaceTypes,
                serviceType,
                createStateManager: createStateManager,
                createStatefulService: (context, manager) => createService(context, manager),
                createStatelessService: null,
                serviceDefinition: serviceDefinition,
                isStateful: true,
                serviceUri: serviceUri,
                serviceName: serviceName);

            var instances = MockServiceInstance.Register(FabricRuntime, serviceRegistration,
                serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
        }


        public void SetupService
        (
            Type serviceType,
            Func<StatefulServiceContext, IReliableStateManagerReplica2,
                StatefulServiceBase> createService,
            CreateStateManager createStateManager = null,
            MockServiceDefinition serviceDefinition = null,
            IServiceManifest serviceManifest = null,
            IServiceConfig serviceConfig = null
        )
        {
            var serviceInterfaceTypes = serviceType.IsInterface
                ? new[] {serviceType}
                : serviceType
                    .GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

            serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

            var serviceName = serviceType.Name;
            var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();


            var serviceRegistration = new MockableServiceRegistration(
                serviceInterfaceTypes,
                serviceType,
                createStateManager: createStateManager,
                createStatefulService: (context, manager) => createService(context, manager),
                createStatelessService: null,
                serviceDefinition: serviceDefinition,
                isStateful: true,
                serviceUri: serviceUri,
                serviceName: serviceName);

            var instances = MockServiceInstance.Register(FabricRuntime, serviceRegistration,
                serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
        }


        public void SetupService
        (
            Type serviceType,
            Func<StatelessServiceContext, StatelessService> createService,
            MockServiceDefinition serviceDefinition = null,
            IServiceManifest serviceManifest = null,
            IServiceConfig serviceConfig = null
        )
        {
            var serviceInterfaceTypes = serviceType.IsInterface
                ? new[] {serviceType}
                : serviceType
                    .GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

            serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
            var serviceName = serviceType.Name;
            var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

            var serviceRegistration = new MockableServiceRegistration(
                serviceInterfaceTypes,
                serviceType,
                createStateManager: null,
                createStatefulService: null,
                createStatelessService: context => createService(context),
                serviceDefinition: serviceDefinition,
                isStateful: false,
                serviceUri: serviceUri,
                serviceName: serviceName);

            var instances = MockServiceInstance.Register(FabricRuntime, serviceRegistration,
                serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
        }

        public void SetupService<TServiceImplementation>
        (
            Func<StatelessServiceContext, TServiceImplementation> createService,
            MockServiceDefinition serviceDefinition = null,
            IServiceManifest serviceManifest = null,
            IServiceConfig serviceConfig = null
        )
            where TServiceImplementation : StatelessService
        {
            var serviceType = typeof(TServiceImplementation);
            var serviceInterfaceTypes = typeof(TServiceImplementation).IsInterface
                ? new[] {typeof(TServiceImplementation)}
                : typeof(TServiceImplementation)
                    .GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

            serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
            var serviceName = serviceType.Name;
            var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

            var serviceRegistration = new MockableServiceRegistration(
                serviceInterfaceTypes,
                serviceType,
                createStateManager: null,
                createStatefulService: null,
                createStatelessService: context => createService(context),
                serviceDefinition: serviceDefinition,
                isStateful: false,
                serviceUri: serviceUri,
                serviceName: serviceName);

            var instances = MockServiceInstance.Register(FabricRuntime, serviceRegistration,
                serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
        }

        public void SetupActor<TActorImplementation, TActorService>
        (
            Func<TActorService, ActorId, TActorImplementation> activator,
            CreateActorService<TActorService> createActorService = null,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
            MockServiceDefinition serviceDefinition = null,
            IServiceManifest serviceManifest = null,
            IServiceConfig serviceConfig = null
        )
            where TActorImplementation : class, IActor
            where TActorService : ActorService
        {
            var actorInterface = typeof(TActorImplementation).IsInterface
                ? typeof(TActorImplementation)
                : typeof(TActorImplementation).GetInterfaces()
                    .FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));

            var serviceInterfaceTypes = typeof(TActorService).IsInterface
                ? new[] {typeof(TActorService)}
                : typeof(TActorService).GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService)))
                    .ToArray();

            serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

            var serviceType = typeof(TActorService);
            var serviceName = serviceType.Name;
            var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

            var serviceRegistration = new MockableServiceRegistration(
                serviceInterfaceTypes,
                serviceType,
                createStateManager: null,
                createStatefulService: null,
                createStatelessService: null,
                serviceDefinition: serviceDefinition,
                isStateful: true,
                serviceUri: serviceUri,
                serviceName: serviceName);

            var actorRegistration = new MockableActorRegistration<TActorService>(
                serviceRegistration,
                actorInterface,
                typeof(TActorImplementation),
                createActorService,
                (Func<ActorService, ActorId, TActorImplementation>) ((actorService, actorId) =>
                    activator((TActorService) actorService, actorId)),
                createActorStateManager,
                createActorStateProvider);

            var instances = MockServiceInstance.Register(FabricRuntime, actorRegistration,
                serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
        }

        public void SetupActor<TActorImplementation>
        (
            Func<ActorService, ActorId, TActorImplementation> activator,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
            MockServiceDefinition serviceDefinition = null,
            IServiceManifest serviceManifest = null,
            IServiceConfig serviceConfig = null)
            where TActorImplementation : class, IActor
        {
            var actorInterface = typeof(TActorImplementation).IsInterface
                ? typeof(TActorImplementation)
                : typeof(TActorImplementation).GetInterfaces()
                    .FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));

            serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

            var serviceName = $"{typeof(TActorImplementation).Name}Service";
            var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

            var serviceRegistration = new MockableServiceRegistration(
                new Type[0],
                typeof(MockActorService),
                createStateManager: null,
                createStatefulService: null,
                createStatelessService: null,
                serviceDefinition: serviceDefinition,
                isStateful: true,
                serviceUri: serviceUri,
                serviceName: serviceName);

            var actorRegistration = new MockableActorRegistration(
                serviceRegistration,
                actorInterface,
                typeof(TActorImplementation),
                activator,
                createActorStateManager: createActorStateManager,
                createActorStateProvider: createActorStateProvider);

            var instances = MockServiceInstance.Register(FabricRuntime, actorRegistration,
                serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
        }

        public void SetupActor
        (
            Type actorImplementationType,
            Type actorServiceImplementationType,
            Func<ActorService, ActorId, object> activator,
            Func<StatefulServiceContext, ActorTypeInformation, IActorStateProvider,
                Func<ActorBase, IActorStateProvider, IActorStateManager>, ActorService> createActorService = null,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
            MockServiceDefinition serviceDefinition = null,
            IServiceManifest serviceManifest = null,
            IServiceConfig serviceConfig = null)
        {
            var actorInterface = actorImplementationType.IsInterface
                ? actorImplementationType
                : actorImplementationType.GetInterfaces()
                    .FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));

            var serviceInterfaceTypes = actorServiceImplementationType.IsInterface
                ? new[] {actorServiceImplementationType}
                : actorServiceImplementationType
                    .GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

            serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

            var serviceType = actorServiceImplementationType;
            var serviceName = serviceType.Name;
            var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

            var serviceRegistration = new MockableServiceRegistration(
                serviceInterfaceTypes,
                serviceType,
                createStateManager: null,
                createStatefulService: null,
                createStatelessService: null,
                serviceDefinition: serviceDefinition,
                isStateful: true,
                serviceUri: serviceUri,
                serviceName: serviceName);


            var actorRegistration = new MockableActorRegistration(
                serviceRegistration,
                actorInterface,
                actorImplementationType,
                createActorService: (context, information, provider, factory) =>
                    createActorService?.Invoke(context, information, provider, factory),
                activator: activator,
                createActorStateManager: createActorStateManager,
                createActorStateProvider: createActorStateProvider);

            var instances = MockServiceInstance.Register(FabricRuntime, actorRegistration,
                serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
        }
    }
}