using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Reflection;
using FG.Common.Utils;
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
using StatefulService = Microsoft.ServiceFabric.Services.Runtime.StatefulService;
using StatelessService = Microsoft.ServiceFabric.Services.Runtime.StatelessService;

namespace FG.ServiceFabric.Testing.Mocks
{
	internal class MockServiceInstance
	{
		public MockFabricRuntime FabricRuntime { get; set; }

		public Uri ServiceUri { get; set; }
		public Partition Partition { get; set; }
		public Replica Replica { get; set; }

		public IMockableServiceRegistration ServiceRegistration { get; set; }
		public IMockableActorRegistration ActorRegistration { get; set; }

		public object ServiceInstance { get; set; }

		private void Build()
		{
			var stateManager = (ServiceRegistration.CreateStateManager ??
			                          (() => (IReliableStateManagerReplica) new MockReliableStateManager(FabricRuntime))).Invoke();			

			var serviceTypeInformation = ServiceTypeInformation.Get(ServiceRegistration.ImplementationType);

			if (ServiceRegistration.IsStateful)
			{
				var statefulServiceContext = FabricRuntime.BuildStatefulServiceContext(ServiceRegistration.Name);
				var serviceFactory = ServiceRegistration.CreateStatefulService ?? GetMockStatefulService;
					// TODO: consider this further, is it really what should be done???

				var statefulService = serviceFactory(statefulServiceContext, serviceTypeInformation, stateManager);
				if (statefulService is FG.ServiceFabric.Services.Runtime.StatefulService)
				{
					statefulService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
					statefulService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
					statefulService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
				}

				ServiceInstance = statefulService;
			}
			else
			{
				var statelessServiceContext = FabricRuntime.BuildStatelessServiceContext(ServiceRegistration.Name);
				var serviceFactory = ServiceRegistration.CreateStatelessService ?? GetMockStatelessService;
				// TODO: consider this further, is it really what should be done???

				var statelessService = serviceFactory(statelessServiceContext, serviceTypeInformation);
				if (statelessService is FG.ServiceFabric.Services.Runtime.StatelessService)
				{
					statelessService.SetPrivateField("_serviceProxyFactory", FabricRuntime.ServiceProxyFactory);
					statelessService.SetPrivateField("_actorProxyFactory", FabricRuntime.ActorProxyFactory);
					statelessService.SetPrivateField("_applicationUriBuilder", FabricRuntime.ApplicationUriBuilder);
				}

				ServiceInstance = statelessService;
			}

		}

		internal void Run()
		{
			if (ServiceRegistration.IsStateful)
			{

			}
			else
			{
				
			}
		}

		private StatelessService GetMockStatelessService(
			StatelessServiceContext serviceContext,
			ServiceTypeInformation serviceTypeInformation)
		{
			return new MockStatelessService(
					codePackageActivationContext: FabricRuntime.CodePackageContext,
					serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
					nodeContext: FabricRuntime.BuildNodeContext(),
					statelessServiceContext: serviceContext,
					serviceTypeInfo: serviceTypeInformation);
		}

		private StatefulService GetMockStatefulService(
			StatefulServiceContext serviceContext,
			ServiceTypeInformation serviceTypeInformation,
			IReliableStateManagerReplica stateManager)
		{
			return new MockStatefulService(
					codePackageActivationContext: FabricRuntime.CodePackageContext,
					serviceProxyFactory: FabricRuntime.ServiceProxyFactory,
					nodeContext: FabricRuntime.BuildNodeContext(),
					statefulServiceContext: serviceContext,
					serviceTypeInfo: serviceTypeInformation,
					stateManager: stateManager);
		}

		public static IEnumerable<MockServiceInstance> Build(
			MockFabricRuntime fabricRuntime,
			IMockableServiceRegistration serviceRegistration
		)
		{
			var instances = new List<MockServiceInstance>();
			foreach (var replica in serviceRegistration.ServiceDefinition.Instances)
			{
				foreach (var partition in serviceRegistration.ServiceDefinition.Partitions)
				{
					var instance = new MockServiceInstance()
					{
						ServiceRegistration = serviceRegistration,
						FabricRuntime = fabricRuntime,
						Partition = partition,
						Replica = replica,
						ServiceUri = serviceRegistration.ServiceUri
					};
					instance.Build();
					instances.Add(instance);
				}
			}

			return instances;
		}
	}



    public class MockFabricRuntime
    {
        private readonly MockReliableStateManager _stateManager;
        private readonly MockServiceProxyFactory _serviceProxyFactory;
        private readonly MockActorProxyFactory _actorProxyFactory;
		private readonly MockPartitionEnumerationManager _partitionEnumerationManager;

		public string ApplicationName { get; private set; }

        public IReliableStateManager StateManager => _stateManager;
        public IReliableStateManagerReplica StateManagerReplica => _stateManager;


		private readonly IDictionary<Uri, IMockableActorRegistration> _actorRegistrations;
		private readonly IDictionary<Uri, IMockableServiceRegistration> _serviceRegistrations;

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

		internal string GetStatelessServiceInstanceKey(Uri serviceUri, long instanceId)
		{
			return $"{serviceUri}?instanceId={instanceId}";
		}

		internal string GetStatefulServicePartitionKey(Uri serviceUri, Guid partitionId)
		{
			return $"{serviceUri}?partitionId={partitionId}";
		}

		public MockFabricRuntime(string applicationName)
        {
            ApplicationName = applicationName;
            _stateManager = new MockReliableStateManager(this);

            var serviceProxyFactory = new MockServiceProxyFactory(this);
            var actorProxyFactory = new MockActorProxyFactory(this);

            _serviceProxyFactory = serviceProxyFactory;
            _actorProxyFactory = actorProxyFactory;

			FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory.SetInnerFactory((factory, serviceType) => _serviceProxyFactory);
	        FG.ServiceFabric.Actors.Client.ActorProxyFactory.SetInnerFactory((factory, serviceType, actorType) => _actorProxyFactory);

			_partitionEnumerationManager = new MockPartitionEnumerationManager(this);

			_actorRegistrations = new Dictionary<Uri, IMockableActorRegistration>();
			_serviceRegistrations = new Dictionary<Uri, IMockableServiceRegistration>();

	        _activeInstances = new List<MockServiceInstance>();
        }

        public void SetupAutoDiscoverActors(params Assembly[] actorAssemblies)
        {
            foreach (var actorAssembly in actorAssemblies)
            {
                foreach (var actorType in actorAssembly.GetTypes().Where(t => typeof(Actor).IsAssignableFrom(t)))
                {
                    var basicConstructor = actorType.GetConstructors()
                        .FirstOrDefault(c => 
                            c.GetParameters().Length == 2 && 
                            c.GetParameters().Any(p => typeof(ActorService).IsAssignableFrom(p.ParameterType)) &&
                            c.GetParameters().Any(p => typeof(ActorId).IsAssignableFrom(p.ParameterType)));

                    var actorInterface = actorType.GetInterfaces()
                        .FirstOrDefault(i => typeof(IActor).IsAssignableFrom(i));

                    if (basicConstructor != null && actorInterface != null)
                    {
                        SetupActorInternal(actorType);
                    }
                }
            }
        }

        private void SetupActorInternal(Type actorImplementationType)
        {
            var actorInterface = actorImplementationType.IsInterface
                ? actorImplementationType
                : actorImplementationType.GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));
            var actorRegistration = new MockableActorRegistration(actorInterface, actorImplementationType,
                (Func<ActorService, ActorId, IActor>)((actorService, actorId) => (IActor) Activator.CreateInstance(actorImplementationType, actorService, actorId)), 
                null, null);
            ((MockActorProxyFactory)ActorProxyFactory).AddActorRegistration(actorRegistration);
        }
		
	    public void SetupService<TServiceImplementation>(
			Func<StatefulServiceContext, ServiceTypeInformation, IReliableStateManagerReplica, TServiceImplementation> createService,
			CreateStateManager createStateManager = null,
			MockServiceDefinition serviceDefinition = null
		) where TServiceImplementation : ServiceFabric.Services.Runtime.StatefulService
		{
		    var serviceType = typeof(TServiceImplementation);
			var serviceInterfaceType = typeof(TServiceImplementation).IsInterface
			 ? typeof(TServiceImplementation)
			 : typeof(TServiceImplementation).GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService)));

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

			var serviceName = ApplicationUriBuilder.Build(serviceType.Name).ToUri();

			var serviceRegistration = new MockableServiceRegistration(
			    interfaceType: serviceInterfaceType,
			    implementationType: serviceType,
			    createStateManager: createStateManager,
			    createStatefulService: ((context, information, manager) => createService(context, information, manager)),
			    createStatelessService: null,
			    serviceDefinition: serviceDefinition,
				isStateful: true);

			_serviceRegistrations.Add(serviceRegistration.ServiceUri, serviceRegistration);

			((MockServiceProxyFactory)ServiceProxyFactory).AddServiceRegistration(serviceRegistration);

			var instances = MockServiceInstance.Build(this, serviceRegistration);
			_activeInstances.AddRange(instances);
		}

		public void SetupService<TServiceImplementation>(
			Func<StatelessServiceContext, ServiceTypeInformation, TServiceImplementation> createService,
			MockServiceDefinition serviceDefinition = null
		) where TServiceImplementation : ServiceFabric.Services.Runtime.StatelessService
		{
			var serviceType = typeof(TServiceImplementation);
			var serviceInterfaceType = typeof(TServiceImplementation).IsInterface
			 ? typeof(TServiceImplementation)
			 : typeof(TServiceImplementation).GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService)));

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

			var serviceName = ApplicationUriBuilder.Build(serviceType.Name).ToUri();

			var serviceRegistration = new MockableServiceRegistration(
				interfaceType: serviceInterfaceType,
				implementationType: serviceType,
				createStateManager: null,
				createStatefulService: null,
				createStatelessService: (context, information) => createService(context, information),
				serviceDefinition: serviceDefinition,
				isStateful: false,
				serviceUri: serviceName);

			_serviceRegistrations.Add(serviceRegistration.ServiceUri, serviceRegistration);

			((MockServiceProxyFactory)ServiceProxyFactory).AddServiceRegistration(serviceRegistration);

			var instances = MockServiceInstance.Build(this, serviceRegistration);
			_activeInstances.AddRange(instances);

		}

		public void SetupActor<TActorImplementation, TActorService>(
            Func<TActorService, ActorId, TActorImplementation> activator,
            CreateActorService<TActorService> createActorService,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
			MockServiceDefinition serviceDefinition = null)
            where TActorImplementation : class, IActor
            where TActorService : ActorService
        {
            var actorInterface = typeof(TActorImplementation).IsInterface
                ? typeof(TActorImplementation)
                : typeof(TActorImplementation).GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));

	        var serviceInterfaces = typeof(TActorService)
				.GetInterfaces()
				.FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IService)));

			serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;

			var serviceName = ApplicationUriBuilder.Build(typeof(TActorImplementation).Name).ToUri();

			var actorRegistration = new MockableActorRegistration<TActorService>(
                interfaceType: actorInterface, 
                implementationType: typeof(TActorImplementation),
                createActorService: createActorService,
                activator: (Func<ActorService, ActorId, TActorImplementation>) ((actorService, actorId) => activator((TActorService)actorService, actorId)), 
                createActorStateManager: createActorStateManager, 
                createActorStateProvider: createActorStateProvider,
				serviceDefinition: serviceDefinition,
				serviceUri: serviceName);

			_actorRegistrations.Add(actorRegistration.ServiceUri, actorRegistration);		

			((MockActorProxyFactory)ActorProxyFactory).AddActorRegistration(actorRegistration);			
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

			var serviceName = ApplicationUriBuilder.Build(typeof(TActorImplementation).Name).ToUri();

			var actorRegistration = new MockableActorRegistration(
				interfaceType: actorInterface,
				implementationType: typeof(TActorImplementation), 
				activator: activator, 
				createActorStateManager: createActorStateManager,
				createActorStateProvider: createActorStateProvider,
				serviceDefinition: serviceDefinition);

			_actorRegistrations.Add(actorRegistration.ServiceUri, actorRegistration);

			((MockActorProxyFactory)ActorProxyFactory).AddActorRegistration(actorRegistration);
        }		
    }
}