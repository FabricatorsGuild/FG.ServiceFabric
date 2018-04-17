using System;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    using Microsoft.ServiceFabric.Actors;

    public class MockableServiceRegistration : IMockableServiceRegistration
    {
        public MockableServiceRegistration(
            Type[] interfaceTypes,
            Type implementationType,
            CreateStatefulService createStatefulService = null,
            CreateStatelessService createStatelessService = null,
            CreateStateManager createStateManager = null,
            MockServiceDefinition serviceDefinition = null,
            bool isStateful = false,
            Uri serviceUri = null,
            string serviceName = null)
        {
            InterfaceTypes = interfaceTypes;
            ImplementationType = implementationType;
            CreateStatefulService = createStatefulService;
            CreateStatelessService = createStatelessService;
            CreateStateManager = createStateManager;
            IsStateful = isStateful;
            ServiceUri = serviceUri;
            ServiceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
            Name = serviceName ?? implementationType.Name;
        }

        public MockableServiceRegistration(
            Type[] interfaceTypes,
            object serviceInstance,
            MockServiceDefinition serviceDefinition = null,
            Uri serviceUri = null,
            string serviceName = null)
        {
            InterfaceTypes = interfaceTypes;
            ServiceUri = serviceUri;
            ServiceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
            Name = serviceName ?? serviceInstance.GetType().Name;
            ServiceInstance = serviceInstance;
        }

        public MockableServiceRegistration(
            Type[] interfaceTypes,
            Func<MockActorContainer, IActorService> actorServiceInstanceFactory,
            MockServiceDefinition serviceDefinition = null,
            Uri serviceUri = null,
            string serviceName = null)
        {
            InterfaceTypes = interfaceTypes;
            this.ActorServiceInstanceFactory = actorServiceInstanceFactory;
            ServiceUri = serviceUri;
            ServiceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
            Name = serviceName;
            

        }

        public bool IsSimple { get; set; }

        public object ServiceInstance { get; set; }

        public Type[] InterfaceTypes { get; }

        public Func<MockActorContainer, IActorService> ActorServiceInstanceFactory { get; }

        public Type ImplementationType { get; }

        public CreateStateManager CreateStateManager { get; }
        public MockServiceDefinition ServiceDefinition { get; set; }
        public CreateStatefulService CreateStatefulService { get; }
        public CreateStatelessService CreateStatelessService { get; }
        public bool IsStateful { get; }
        public Uri ServiceUri { get; set; }
        public string Name { get; set; }
    }
}