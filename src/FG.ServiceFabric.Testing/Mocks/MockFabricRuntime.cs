using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using FG.Common.Utils;
using FG.ServiceFabric.Fabric;
using FG.ServiceFabric.Fabric.Runtime;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks
{
	public class MockFabricRuntime
	{
		private readonly List<MockServiceInstance> _activeInstances;
		private readonly MockActorProxyFactory _actorProxyFactory;
		private readonly List<MockFabricApplication> _applications;
		private readonly MockPartitionEnumerationManager _partitionEnumerationManager;
		private readonly MockServiceProxyFactory _serviceProxyFactory;

		public MockFabricRuntime()
		{
			var serviceProxyFactory = new MockServiceProxyFactory(this);
			var actorProxyFactory = new MockActorProxyFactory(this);

			_serviceProxyFactory = serviceProxyFactory;
			_actorProxyFactory = actorProxyFactory;

			FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory.SetInnerFactory((factory, serviceType) =>
				_serviceProxyFactory);
			FG.ServiceFabric.Actors.Client.ActorProxyFactory.SetInnerFactory((factory, serviceType, actorType) =>
				_actorProxyFactory);

			_partitionEnumerationManager = new MockPartitionEnumerationManager(this);

			_applications = new List<MockFabricApplication>();
			_activeInstances = new List<MockServiceInstance>();

			CancellationTokenSource = new CancellationTokenSource();
			CancellationToken = CancellationTokenSource.Token;
		}

		public CancellationToken CancellationToken { get; private set; }
		public CancellationTokenSource CancellationTokenSource { get; private set; }

		public static MockFabricRuntime Current
		{
			get => (MockFabricRuntime) FabricRuntimeContextWrapper.Current?[FabricRuntimeContextKeys.CurrentMockFabricRuntime];
			set => FabricRuntimeContextWrapper.Current[FabricRuntimeContextKeys.CurrentMockFabricRuntime] = value;
		}


		public bool DisableMethodCallOutput { get; set; }

		internal IEnumerable<MockServiceInstance> Instances => _activeInstances;

		public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory;
		public IActorProxyFactory ActorProxyFactory => _actorProxyFactory;
		public IPartitionEnumerationManager PartitionEnumerationManager => _partitionEnumerationManager;

		internal ICodePackageActivationContext GetCodePackageContext(string applicationName)
		{
			return new MockCodePackageActivationContext(
				applicationName: $"fabric:/{applicationName}",
				applicationTypeName: $"{applicationName}Type",
				codePackageName: "Code",
				codePackageVersion: "1.0.0.0",
				context: Guid.NewGuid().ToString(),
				logDirectory: @"C:\Log",
				tempDirectory: @"C:\Temp",
				workDirectory: @"C:\Work",
				serviceManifestName: "ServiceManifest",
				serviceManifestVersion: "1.0.0.0"
			);
		}

		internal ApplicationUriBuilder GetApplicationUriBuilder(string applicationName)
		{
			return new ApplicationUriBuilder(GetCodePackageContext(applicationName));
		}

		internal void RegisterInstances(IEnumerable<MockServiceInstance> instances)
		{
			var mockServiceInstances = instances.ToArray();
			foreach (var instance in mockServiceInstances)
			{
				if (_activeInstances.Any(i => i.ServiceUri.Equals(instance.ServiceUri)))
				{
					throw new MockFabricSetupException($"Trying to register services with {instance.ServiceUri}, but MockFabricRuntime already has a registration for that ServiceUri");
				}
			}

			_activeInstances.AddRange(mockServiceInstances);
		}

		public IEnumerable<IMockServiceInstance> GetInstances()
		{
			return _activeInstances.Select(i => i as IMockServiceInstance).ToArray();
		}

		internal NodeContext BuildNodeContext()
		{
			return new NodeContext("NODE_1", new NodeId(1L, 5L), 1L, "NODE_TYPE_1", "10.0.0.1");
		}

		public StatefulServiceContext BuildStatefulServiceContext(string applicationName, string serviceName,
			ServicePartitionInformation partitionInformation, long replicaId)
		{
			return new StatefulServiceContext(
				new NodeContext(
					"Node0",
					nodeId: new NodeId(0, 1),
					nodeInstanceId: 0,
					nodeType: "NodeType1",
					ipAddressOrFQDN: "TEST.MACHINE"),
				codePackageActivationContext: GetCodePackageContext(applicationName),
				serviceTypeName: $"{serviceName}Type",
				serviceName: new Uri($"fabric:/{applicationName}/{serviceName}"),
				initializationData: null,
				partitionId: partitionInformation.Id,
				replicaId: replicaId
			);
		}

		public StatelessServiceContext BuildStatelessServiceContext(string applicationName, string serviceName)
		{
			return new StatelessServiceContext(
				new NodeContext(
					"Node0",
					nodeId: new NodeId(0, 1),
					nodeInstanceId: 0,
					nodeType: "NodeType1",
					ipAddressOrFQDN: "TEST.MACHINE"),
				codePackageActivationContext: GetCodePackageContext(applicationName),
				serviceTypeName: serviceName,
				serviceName: new Uri($"fabric:/{applicationName}/{serviceName}"),
				initializationData: null,
				partitionId: Guid.NewGuid(),
				instanceId: 1L
			);
		}

		public MockFabricApplication GetApplication(string applicationInstanceName)
		{
			var existingApplication = _applications.FirstOrDefault(a =>
				a.ApplicationInstanceName.Equals(applicationInstanceName, StringComparison.InvariantCulture));
			if (existingApplication == null)
			{
				throw new ArgumentException(
					$"An application with name {applicationInstanceName} could not be found this runtime, register one with {nameof(RegisterApplication)}");
			}

			return existingApplication;
		}

		public MockFabricApplication RegisterApplication(string applicationInstanceName)
		{
			var existingApplication = _applications.FirstOrDefault(a =>
				a.ApplicationInstanceName.Equals(applicationInstanceName, StringComparison.InvariantCulture));
			if (existingApplication != null)
			{
				throw new ArgumentException(
					$"An application with name {applicationInstanceName} is already registered in this runtime");
			}

			var mockFabricApplication = new MockFabricApplication(this, applicationInstanceName);

			_applications.Add(mockFabricApplication);
			return mockFabricApplication;
		}

		private static class FabricRuntimeContextKeys
		{
			public const string CurrentMockFabricRuntime = "mockFabricRuntime";
		}
	}

	public class MockFabricApplication
	{
		private readonly string _applicationInstanceName;
		private readonly MockFabricRuntime _mockFabricRuntime;
		private ApplicationUriBuilder _applicationUriBuilder;
		public ICodePackageActivationContext _codePackageActivationContext;

		internal MockFabricApplication(MockFabricRuntime mockFabricRuntime, string applicationInstanceName)
		{
			_mockFabricRuntime = mockFabricRuntime;
			_applicationInstanceName = applicationInstanceName;
			_applicationUriBuilder = _mockFabricRuntime.GetApplicationUriBuilder(_applicationInstanceName);
			_codePackageActivationContext = _mockFabricRuntime.GetCodePackageContext(_applicationInstanceName);
		}

		public MockFabricRuntime FabricRuntime => _mockFabricRuntime;
		public string ApplicationInstanceName => _applicationInstanceName;

		public ICodePackageActivationContext CodePackageContext => _codePackageActivationContext;

		public ApplicationUriBuilder ApplicationUriBuilder => _applicationUriBuilder;

		public void SetupService<TServiceImplementation>
		(
			Func<StatefulServiceContext, IReliableStateManagerReplica2, TServiceImplementation> createService,
			CreateStateManager createStateManager = null,
			MockServiceDefinition serviceDefinition = null
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

			var instances = MockServiceInstance.Register(_mockFabricRuntime, serviceRegistration);
		}


		public void SetupService
		(
			Type serviceType,
			Func<StatefulServiceContext, IReliableStateManagerReplica2,
				Microsoft.ServiceFabric.Services.Runtime.StatefulServiceBase> createService,
			CreateStateManager createStateManager = null,
			MockServiceDefinition serviceDefinition = null
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

			var instances = MockServiceInstance.Register(_mockFabricRuntime, serviceRegistration);
		}


		public void SetupService
		(
			Type serviceType,
			Func<StatelessServiceContext, Microsoft.ServiceFabric.Services.Runtime.StatelessService> createService,
			MockServiceDefinition serviceDefinition = null
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

			var instances = MockServiceInstance.Register(_mockFabricRuntime, serviceRegistration);
		}

		public void SetupService<TServiceImplementation>
		(
			Func<StatelessServiceContext, TServiceImplementation> createService,
			MockServiceDefinition serviceDefinition = null
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

			var instances = MockServiceInstance.Register(_mockFabricRuntime, serviceRegistration);
		}

		public void SetupActor<TActorImplementation, TActorService>
		(
			Func<TActorService, ActorId, TActorImplementation> activator,
			CreateActorService<TActorService> createActorService = null,
			CreateActorStateManager createActorStateManager = null,
			CreateActorStateProvider createActorStateProvider = null,
			MockServiceDefinition serviceDefinition = null
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

			var instances = MockServiceInstance.Register(_mockFabricRuntime, actorRegistration);
		}

		public void SetupActor<TActorImplementation>
		(
			Func<ActorService, ActorId, TActorImplementation> activator,
			CreateActorStateManager createActorStateManager = null,
			CreateActorStateProvider createActorStateProvider = null,
			MockServiceDefinition serviceDefinition = null)
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

			var instances = MockServiceInstance.Register(_mockFabricRuntime, actorRegistration);
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
			MockServiceDefinition serviceDefinition = null)
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

			var instances = MockServiceInstance.Register(_mockFabricRuntime, actorRegistration);
		}
	}
}