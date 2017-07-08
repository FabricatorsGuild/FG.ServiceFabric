using System;
using System.Fabric;
using System.Linq;
using System.Reflection;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Data;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
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
        private readonly MockReliableStateManager _stateManager;
        private readonly IServiceProxyFactory _serviceProxyFactory;
        private readonly IActorProxyFactory _actorProxyFactory;

        public string ApplicationName { get; set; }

        public IReliableStateManager StateManager => _stateManager;
        public IReliableStateManagerReplica StateManagerReplica => _stateManager;
        public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory;
        public IActorProxyFactory ActorProxyFactory => _actorProxyFactory;

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


        public IActorStateProvider GetActorStateProvider<TActorImplemenationType>()
            where TActorImplemenationType : IActor
        {
            return GetActorStateProvider(typeof(TActorImplemenationType));
        }

        public IActorStateProvider GetActorStateProvider(Type actorImplementationType)
        {
            var mockActorProxyFactory = ActorProxyFactory as MockActorProxyFactory;
            return mockActorProxyFactory?.GetActorStateProvider(actorImplementationType);
        }

        public TActor GetActor<TActor>(IActor proxy)
            where TActor : class
        {
            var mockActorProxyFactory = ActorProxyFactory as MockActorProxyFactory;
            return mockActorProxyFactory?.GetActor<TActor>(proxy);
        }


        public TService GetService<TService>(Uri serviceUri)
            where TService : class
        {
            var serviceProxyFactory = ServiceProxyFactory as MockServiceProxyFactory;
            return serviceProxyFactory?.GetService<TService>(serviceUri);
        }

        public MockFabricRuntime(string applicationName)
        {
            ApplicationName = applicationName;
            _stateManager = new MockReliableStateManager();

            var serviceProxyFactory = new MockServiceProxyFactory(this);
            var actorProxyFactory = new MockActorProxyFactory(this);

            _serviceProxyFactory = serviceProxyFactory;
            _actorProxyFactory = actorProxyFactory;

			FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory.SetInnerFactory((factory, serviceType) => _serviceProxyFactory); ;
			FG.ServiceFabric.Actors.Client.ActorProxyFactory.SetInnerFactory((factory, serviceType, actorType) => _actorProxyFactory);;
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

        public void SetupService<TService>(string serviceName, Func<StatelessServiceContext, IReliableStateManagerReplica, TService> activator = null)
            where TService : FG.ServiceFabric.Services.Runtime.StatelessService, IService
        {
            var serviceUriBuilder = this.ApplicationUriBuilder.Build(serviceName);
            var statefulServiceContext = BuildStatelessServiceContext(serviceName);

            TService serviceInstance = null;
            if (activator == null)
            {
                try
                {
                    serviceInstance = (TService)Activator.CreateInstance(typeof(TService), statefulServiceContext, StateManagerReplica);
                }
                catch (Exception)
                {
                }
                if (serviceInstance == null)
                {
                    try
                    {
                        serviceInstance = (TService)Activator.CreateInstance(typeof(TService), statefulServiceContext);
                    }
                    catch (Exception)
                    {
                        throw new NotSupportedException($"Service should have a constructor that takes {typeof(StatefulServiceContext).Name} and optionally {typeof(IReliableStateManagerReplica).Name} as arguments. If not, specify a custom activator for this Service type.");
                    }
                }
            }
            else
            {
                serviceInstance = activator(statefulServiceContext, StateManagerReplica);
            }
            ((MockServiceProxyFactory)ServiceProxyFactory).AssociateMockServiceAndName(serviceUriBuilder.ToUri(), serviceInstance);
        }

        public void SetupService<TService>(string serviceName, Func<StatefulServiceContext, IReliableStateManagerReplica, TService> activator = null)
            where TService : StatefulService, IService
        {
            var serviceUriBuilder = this.ApplicationUriBuilder.Build(serviceName);
            var statefulServiceContext = BuildStatefulServiceContext(serviceName);

            TService serviceInstance = null;
            if (activator == null)
            {
                try
                {
                    serviceInstance = (TService)Activator.CreateInstance(typeof(TService), statefulServiceContext, StateManagerReplica);
                }
                catch (Exception)
                {
                }
                if (serviceInstance == null)
                {
                    try
                    {
                        serviceInstance = (TService) Activator.CreateInstance(typeof(TService), statefulServiceContext);
                    }
                    catch (Exception)
                    {
                        throw new NotSupportedException($"Service should have a constructor that takes {typeof(StatefulServiceContext).Name} and optionally {typeof(IReliableStateManagerReplica).Name} as arguments. If not, specify a custom activator for this Service type.");
                    }
                }
            }
            else
            {
                serviceInstance = activator(statefulServiceContext, StateManagerReplica);
            }
            ((MockServiceProxyFactory)ServiceProxyFactory).AssociateMockServiceAndName(serviceUriBuilder.ToUri(), serviceInstance);
        }

        public void SetupActor<TActorImplementation, TActorService>(
            Func<TActorService, ActorId, TActorImplementation> activator,
            CreateActorService<TActorService> createActorService,
            CreateStateManager createStateManager = null,
            CreateStateProvider createStateProvider = null)
            where TActorImplementation : class, IActor
            where TActorService : ActorService
        {
            var actorInterface = typeof(TActorImplementation).IsInterface
                ? typeof(TActorImplementation)
                : typeof(TActorImplementation).GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));
            var actorRegistration = new MockableActorRegistration<TActorService>(
                interfaceType: actorInterface, 
                implementationType: typeof(TActorImplementation),
                createActorService: createActorService,
                activator: (Func<ActorService, ActorId, TActorImplementation>) ((actorService, actorId) => activator((TActorService)actorService, actorId)), 
                createStateManager: createStateManager, 
                createStateProvider: createStateProvider);
            ((MockActorProxyFactory)ActorProxyFactory).AddActorRegistration(actorRegistration);
        }

        public void SetupActor<TActorImplementation>(
            Func<ActorService, ActorId, TActorImplementation> activator, 
            CreateStateManager createStateManager = null, 
            CreateStateProvider createStateProvider = null)
            where TActorImplementation : class, IActor
        {
            var actorInterface = typeof(TActorImplementation).IsInterface
                ? typeof(TActorImplementation)
                : typeof(TActorImplementation).GetInterfaces().FirstOrDefault(i => i.GetInterfaces().Any(i2 => i2 == typeof(IActor)));
            var actorRegistration = new MockableActorRegistration(actorInterface, typeof(TActorImplementation), activator, createStateManager, createStateProvider);
            ((MockActorProxyFactory)ActorProxyFactory).AddActorRegistration(actorRegistration);
        }

        public void SetupActor(IMockableActorRegistration actorRegistration)
        {
            ((MockActorProxyFactory)ActorProxyFactory).AddActorRegistration(actorRegistration);
        }
    }
}