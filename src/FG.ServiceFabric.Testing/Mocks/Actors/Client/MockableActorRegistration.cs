using System;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    public class MockableActorRegistration<TActorInterface, TActorImplementation> : IMockableActorRegistration
        where TActorInterface : IActor
        where TActorImplementation : class, TActorInterface
    {
        public MockableActorRegistration(
            IMockableServiceRegistration serviceRegistration,
            Func<ActorService, ActorId, TActorImplementation> activator,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
            MockServiceDefinition serviceDefinition = null,
            Uri serviceUri = null)
        {
            InterfaceType = typeof(TActorInterface);
            ImplementationType = typeof(TActorImplementation);
            Activator = activator;
            CreateActorStateManager = createActorStateManager;
            CreateActorStateProvider = createActorStateProvider;
            CreateActorService = null;
            ServiceRegistration = serviceRegistration;
        }

        public Type InterfaceType { get; }
        public Type ImplementationType { get; }
        public Func<ActorService, ActorId, object> Activator { get; }
        public CreateActorStateManager CreateActorStateManager { get; }
        public CreateActorStateProvider CreateActorStateProvider { get; }
        public CreateActorService CreateActorService { get; }
        public IMockableServiceRegistration ServiceRegistration { get; set; }
    }

    public class MockableActorRegistration : IMockableActorRegistration
    {
        public MockableActorRegistration(
            IMockableServiceRegistration serviceRegistration,
            Type interfaceType,
            Type implementationType,
            Func<ActorService, ActorId, object> activator,
            CreateActorService createActorService = null,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            Activator = activator;
            CreateActorService = createActorService;
            CreateActorStateManager = createActorStateManager;
            CreateActorStateProvider = createActorStateProvider;
            ServiceRegistration = serviceRegistration;


            //ServiceUri = serviceUri;
            //CreateActorService = null;
            //ServiceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
            //Name = serviceName ?? $"{ImplementationType.Name}Service";
        }

        public Type InterfaceType { get; }
        public Type ImplementationType { get; }
        public Func<ActorService, ActorId, object> Activator { get; }
        public CreateActorStateManager CreateActorStateManager { get; }
        public CreateActorStateProvider CreateActorStateProvider { get; }
        public CreateActorService CreateActorService { get; }
        public IMockableServiceRegistration ServiceRegistration { get; set; }
    }

    public class MockableActorRegistration<TActorService> : IMockableActorRegistration
        where TActorService : ActorService
    {
        public MockableActorRegistration(
            IMockableServiceRegistration serviceRegistration,
            Type interfaceType,
            Type implementationType,
            CreateActorService<TActorService> createActorService,
            Func<TActorService, ActorId, object> activator,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null
        )
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            CreateActorService = (context, actorTypeInformation, actorStateProvider, stateManagerFactory) =>
                createActorService(context, actorTypeInformation, actorStateProvider, stateManagerFactory);
            Activator = (service, actorId) =>
                activator((TActorService) service, actorId);
            CreateActorStateManager = createActorStateManager;
            CreateActorStateProvider = createActorStateProvider;

            ServiceRegistration = serviceRegistration;
        }

        public Type InterfaceType { get; }
        public Type ImplementationType { get; }
        public Func<ActorService, ActorId, object> Activator { get; }
        public CreateActorStateManager CreateActorStateManager { get; }
        public CreateActorStateProvider CreateActorStateProvider { get; }
        public CreateActorService CreateActorService { get; }
        public IMockableServiceRegistration ServiceRegistration { get; set; }
    }
}