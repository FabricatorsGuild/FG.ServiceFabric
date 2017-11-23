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

namespace FG.ServiceFabric.Testing.Mocks
{
	public class MockFabricApplication
	{
		private readonly string _applicationInstanceName;
		private readonly MockFabricRuntime _mockFabricRuntime;
		private readonly ApplicationUriBuilder _applicationUriBuilder;

		internal MockFabricApplication(MockFabricRuntime mockFabricRuntime, string applicationInstanceName)
		{
			_mockFabricRuntime = mockFabricRuntime;
			_applicationInstanceName = applicationInstanceName;
			_applicationUriBuilder = _mockFabricRuntime.GetApplicationUriBuilder(_applicationInstanceName);
		}

		public MockFabricRuntime FabricRuntime => _mockFabricRuntime;
		public string ApplicationInstanceName => _applicationInstanceName;

		public ApplicationUriBuilder ApplicationUriBuilder => _applicationUriBuilder;

		public void SetupService<TServiceImplementation>
		(
			Func<StatefulServiceContext, IReliableStateManagerReplica2, TServiceImplementation> createService,
			CreateStateManager createStateManager = null,
			MockServiceDefinition serviceDefinition = null,
			IServiceManifest serviceManifest = null,
			IServiceConfig serviceConfig = null
		)
			where TServiceImplementation : Microsoft.ServiceFabric.Services.Runtime.StatefulServiceBase
		{
			var serviceType = typeof(TServiceImplementation);
			var serviceInterfaceTypes = typeof(TServiceImplementation).IsInterface
				? new Type[] {typeof(TServiceImplementation)}
				: typeof(TServiceImplementation)
					.GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

			var serviceName = serviceType.Name;
			var serviceUri = _applicationUriBuilder.Build(serviceName).ToUri();

			var serviceRegistration = new MockableServiceRegistration(
				interfaceTypes: serviceInterfaceTypes,
				implementationType: serviceType,
				createStateManager: createStateManager,
				createStatefulService: ((context, manager) => createService(context, manager)),
				createStatelessService: null,
				serviceDefinition: serviceDefinition,
				isStateful: true,
				serviceUri: serviceUri,
				serviceName: serviceName);

			var instances = MockServiceInstance.Register(_mockFabricRuntime, serviceRegistration, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
		}


		public void SetupService
		(
			Type serviceType,
			Func<StatefulServiceContext, IReliableStateManagerReplica2,
				Microsoft.ServiceFabric.Services.Runtime.StatefulServiceBase> createService,
			CreateStateManager createStateManager = null,
			MockServiceDefinition serviceDefinition = null,
			IServiceManifest serviceManifest = null,
			IServiceConfig serviceConfig = null
		)
		{
			var serviceInterfaceTypes = serviceType.IsInterface
				? new Type[] {serviceType}
				: serviceType
					.GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

			var serviceName = serviceType.Name;
			var serviceUri = _applicationUriBuilder.Build(serviceName).ToUri();


			var serviceRegistration = new MockableServiceRegistration(
				interfaceTypes: serviceInterfaceTypes,
				implementationType: serviceType,
				createStateManager: createStateManager,
				createStatefulService: ((context, manager) => createService(context, manager)),
				createStatelessService: null,
				serviceDefinition: serviceDefinition,
				isStateful: true,
				serviceUri: serviceUri,
				serviceName: serviceName);

			var instances = MockServiceInstance.Register(_mockFabricRuntime, serviceRegistration, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
		}


		public void SetupService
		(
			Type serviceType,
			Func<StatelessServiceContext, Microsoft.ServiceFabric.Services.Runtime.StatelessService> createService,
			MockServiceDefinition serviceDefinition = null,
			IServiceManifest serviceManifest = null,
			IServiceConfig serviceConfig = null
		)
		{
			var serviceInterfaceTypes = serviceType.IsInterface
				? new Type[] {serviceType}
				: serviceType
					.GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
			var serviceName = serviceType.Name;
			var serviceUri = _applicationUriBuilder.Build(serviceName).ToUri();

			var serviceRegistration = new MockableServiceRegistration(
				interfaceTypes: serviceInterfaceTypes,
				implementationType: serviceType,
				createStateManager: null,
				createStatefulService: null,
				createStatelessService: (context) => createService(context),
				serviceDefinition: serviceDefinition,
				isStateful: false,
				serviceUri: serviceUri,
				serviceName: serviceName);

			var instances = MockServiceInstance.Register(_mockFabricRuntime, serviceRegistration, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
		}

		public void SetupService<TServiceImplementation>
		(
			Func<StatelessServiceContext, TServiceImplementation> createService,
			MockServiceDefinition serviceDefinition = null,
			IServiceManifest serviceManifest = null,
			IServiceConfig serviceConfig = null
		)
			where TServiceImplementation : Microsoft.ServiceFabric.Services.Runtime.StatelessService
		{
			var serviceType = typeof(TServiceImplementation);
			var serviceInterfaceTypes = typeof(TServiceImplementation).IsInterface
				? new Type[] {typeof(TServiceImplementation)}
				: typeof(TServiceImplementation)
					.GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
			var serviceName = serviceType.Name;
			var serviceUri = _applicationUriBuilder.Build(serviceName).ToUri();

			var serviceRegistration = new MockableServiceRegistration(
				interfaceTypes: serviceInterfaceTypes,
				implementationType: serviceType,
				createStateManager: null,
				createStatefulService: null,
				createStatelessService: (context) => createService(context),
				serviceDefinition: serviceDefinition,
				isStateful: false,
				serviceUri: serviceUri,
				serviceName: serviceName);

			var instances = MockServiceInstance.Register(_mockFabricRuntime, serviceRegistration, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
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
				? new Type[] {typeof(TActorService)}
				: typeof(TActorService).GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

			var serviceType = typeof(TActorService);
			var serviceName = serviceType.Name;
			var serviceUri = _applicationUriBuilder.Build(serviceName).ToUri();

			var serviceRegistration = new MockableServiceRegistration(
				interfaceTypes: serviceInterfaceTypes,
				implementationType: serviceType,
				createStateManager: null,
				createStatefulService: null,
				createStatelessService: null,
				serviceDefinition: serviceDefinition,
				isStateful: true,
				serviceUri: serviceUri,
				serviceName: serviceName);

			var actorRegistration = new MockableActorRegistration<TActorService>(
				serviceRegistration,
				interfaceType: actorInterface,
				implementationType: typeof(TActorImplementation),
				createActorService: createActorService,
				activator: (Func<ActorService, ActorId, TActorImplementation>) ((actorService, actorId) =>
					activator((TActorService) actorService, actorId)),
				createActorStateManager: createActorStateManager,
				createActorStateProvider: createActorStateProvider);

			var instances = MockServiceInstance.Register(_mockFabricRuntime, actorRegistration, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
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
			var serviceUri = _applicationUriBuilder.Build(serviceName).ToUri();

			var serviceRegistration = new MockableServiceRegistration(
				interfaceTypes: new Type[0],
				implementationType: typeof(MockActorService),
				createStateManager: null,
				createStatefulService: null,
				createStatelessService: null,
				serviceDefinition: serviceDefinition,
				isStateful: true,
				serviceUri: serviceUri,
				serviceName: serviceName);

			var actorRegistration = new MockableActorRegistration(
				serviceRegistration,
				interfaceType: actorInterface,
				implementationType: typeof(TActorImplementation),
				activator: activator,
				createActorStateManager: createActorStateManager,
				createActorStateProvider: createActorStateProvider);

			var instances = MockServiceInstance.Register(_mockFabricRuntime, actorRegistration, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
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
				: actorImplementationType.GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));

			var serviceInterfaceTypes = actorServiceImplementationType.IsInterface
				? new Type[] {actorServiceImplementationType}
				: actorServiceImplementationType
					.GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

			var serviceType = actorServiceImplementationType;
			var serviceName = serviceType.Name;
			var serviceUri = _applicationUriBuilder.Build(serviceName).ToUri();

			var serviceRegistration = new MockableServiceRegistration(
				interfaceTypes: serviceInterfaceTypes,
				implementationType: serviceType,
				createStateManager: null,
				createStatefulService: null,
				createStatelessService: null,
				serviceDefinition: serviceDefinition,
				isStateful: true,
				serviceUri: serviceUri,
				serviceName: serviceName);


			var actorRegistration = new MockableActorRegistration(
				serviceRegistration,
				interfaceType: actorInterface,
				implementationType: actorImplementationType,
				createActorService: (context, information, provider, factory) =>
					createActorService?.Invoke(context, information, provider, factory),
				activator: activator,
				createActorStateManager: createActorStateManager,
				createActorStateProvider: createActorStateProvider);

			var instances = MockServiceInstance.Register(_mockFabricRuntime, actorRegistration, serviceManifest.OrDefaultFor(serviceName), serviceConfig.OrDefault());
		}
	}
}