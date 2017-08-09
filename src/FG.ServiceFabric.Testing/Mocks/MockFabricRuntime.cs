using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Threading;
using FG.ServiceFabric.Fabric;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Data;
using FG.ServiceFabric.Testing.Mocks.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks
{
	public class MockFabricRuntime
    {
        private readonly MockServiceProxyFactory _serviceProxyFactory;
        private readonly MockActorProxyFactory _actorProxyFactory;
		private readonly MockPartitionEnumerationManager _partitionEnumerationManager;

		public CancellationToken CancellationToken { get; private set; }

		public string ApplicationName { get; private set; }

		private readonly List<MockServiceInstance> _activeInstances;

	    internal IEnumerable<MockServiceInstance> Instances => _activeInstances;

		public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory;
        public IActorProxyFactory ActorProxyFactory => _actorProxyFactory;
	    public IPartitionEnumerationManager PartitionEnumerationManager => _partitionEnumerationManager;

        public ICodePackageActivationContext CodePackageContext => new MockCodePackageActivationContext(
            ApplicationName:  $"fabric:/{ApplicationName}",
            ApplicationTypeName: $"{ApplicationName}Type",
            CodePackageName: "Code",
            CodePackageVersion: "1.0.0.0",
            Context: Guid.NewGuid().ToString(),
            LogDirectory: @"C:\Log",
            TempDirectory: @"C:\Temp",
            WorkDirectory: @"C:\Work",
            ServiceManifestName: "ServiceManifest",
            ServiceManifestVersion: "1.0.0.0"
        );

        public NodeContext BuildNodeContext()
        {
            return new NodeContext("NODE_1", new NodeId(1L, 5L), 1L, "NODE_TYPE_1", "10.0.0.1");
        }

        public StatefulServiceContext BuildStatefulServiceContext(string serviceName)
        {
            return new StatefulServiceContext(
                new NodeContext("Node0", 
                    nodeId: new NodeId(0, 1), 
                    nodeInstanceId: 0, 
                    nodeType: "NodeType1",
                    ipAddressOrFQDN: "TEST.MACHINE"),
                codePackageActivationContext: CodePackageContext,
                serviceTypeName: serviceName,
                serviceName: new Uri($"{CodePackageContext.ApplicationName}/{serviceName}"),
                initializationData: null,
                partitionId: Guid.NewGuid(),
                replicaId: long.MaxValue
            );
        }
        public StatelessServiceContext BuildStatelessServiceContext(string serviceName)
        {
            return new StatelessServiceContext(
                new NodeContext("Node0",
                    nodeId: new NodeId(0, 1),
                    nodeInstanceId: 0,
                    nodeType: "NodeType1",
                    ipAddressOrFQDN: "TEST.MACHINE"),
                codePackageActivationContext: CodePackageContext,
                serviceTypeName: serviceName,
                serviceName: new Uri($"{CodePackageContext.ApplicationName}/{serviceName}"),
                initializationData: null,
                partitionId: Guid.NewGuid(),
                instanceId: 1L
            );
        }

        public ApplicationUriBuilder ApplicationUriBuilder => new ApplicationUriBuilder(CodePackageContext, ApplicationName);
		
		public MockFabricRuntime(string applicationName)
        {
            ApplicationName = applicationName;

            var serviceProxyFactory = new MockServiceProxyFactory(this);
            var actorProxyFactory = new MockActorProxyFactory(this);

            _serviceProxyFactory = serviceProxyFactory;
            _actorProxyFactory = actorProxyFactory;

			FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory.SetInnerFactory((factory, serviceType) => _serviceProxyFactory);
	        FG.ServiceFabric.Actors.Client.ActorProxyFactory.SetInnerFactory((factory, serviceType, actorType) => _actorProxyFactory);

			_partitionEnumerationManager = new MockPartitionEnumerationManager(this);

	        _activeInstances = new List<MockServiceInstance>();

			CancellationToken = new CancellationToken();
        }
		
	    public void SetupService<TServiceImplementation>(
			Func<StatefulServiceContext, IReliableStateManagerReplica, TServiceImplementation> createService,
			CreateStateManager createStateManager = null,
			MockServiceDefinition serviceDefinition = null
		) where TServiceImplementation : Microsoft.ServiceFabric.Services.Runtime.StatefulService
		{
		    var serviceType = typeof(TServiceImplementation);
			var serviceInterfaceTypes = typeof(TServiceImplementation).IsInterface
			 ? new Type[] {typeof(TServiceImplementation)}
			 : typeof(TServiceImplementation).GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

			var serviceName = serviceType.Name;
			var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

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

			var instances = MockServiceInstance.Build(this, serviceRegistration);
			_activeInstances.AddRange(instances);
		}

		public void SetupService<TServiceImplementation>(
			Func<StatelessServiceContext, TServiceImplementation> createService,
			MockServiceDefinition serviceDefinition = null
		) where TServiceImplementation : Microsoft.ServiceFabric.Services.Runtime.StatelessService
		{
			var serviceType = typeof(TServiceImplementation);
			var serviceInterfaceTypes = typeof(TServiceImplementation).IsInterface
			 ? new Type[] { typeof(TServiceImplementation) }
			 : typeof(TServiceImplementation).GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
			var serviceName = serviceType.Name;
			var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

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

			var instances = MockServiceInstance.Build(this, serviceRegistration);
			_activeInstances.AddRange(instances);

		}

		public void SetupActor<TActorImplementation, TActorService>(
            Func<TActorService, ActorId, TActorImplementation> activator,
            CreateActorService<TActorService> createActorService = null,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
			MockServiceDefinition serviceDefinition = null)
            where TActorImplementation : class, IActor
            where TActorService : ActorService
        {
            var actorInterface = typeof(TActorImplementation).IsInterface
                ? typeof(TActorImplementation)
                : typeof(TActorImplementation).GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));

			var serviceInterfaceTypes = typeof(TActorService).IsInterface
			 ? new Type[] { typeof(TActorService) }
			 : typeof(TActorService).GetInterfaces().Where(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService))).ToArray();

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

	        var serviceType = typeof(TActorService);
			var serviceName = serviceType.Name;
			var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

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
                activator: (Func<ActorService, ActorId, TActorImplementation>) ((actorService, actorId) => activator((TActorService)actorService, actorId)), 
                createActorStateManager: createActorStateManager, 
                createActorStateProvider: createActorStateProvider);

			var instances = MockServiceInstance.Build(this, actorRegistration);
			_activeInstances.AddRange(instances);
		}

        public void SetupActor<TActorImplementation>(
            Func<ActorService, ActorId, TActorImplementation> activator, 
            CreateActorStateManager createActorStateManager = null, 
            CreateActorStateProvider createActorStateProvider = null,
			MockServiceDefinition serviceDefinition = null)
            where TActorImplementation : class, IActor
        {
            var actorInterface = typeof(TActorImplementation).IsInterface
                ? typeof(TActorImplementation)
                : typeof(TActorImplementation).GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));
			
			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

	        var serviceName = $"{typeof(TActorImplementation).Name}Service";
			var serviceUri = ApplicationUriBuilder.Build(serviceName).ToUri();

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

			var instances = MockServiceInstance.Build(this, actorRegistration);
			_activeInstances.AddRange(instances);
		}		
    }
}