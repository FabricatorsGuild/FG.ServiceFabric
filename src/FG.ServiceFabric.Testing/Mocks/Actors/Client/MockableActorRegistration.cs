using System;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    public class MockableActorRegistration<TActorInterface, TActorImplementation> : IMockableActorRegistration
        where TActorInterface : IActor
        where TActorImplementation : class, TActorInterface
    {
        public MockableActorRegistration(
            Func<ActorService, ActorId, TActorImplementation> activator,
            CreateStateManager createStateManager = null,
            CreateStateProvider createStateProvider = null)
        {
            InterfaceType = typeof(TActorInterface);
            ImplementationType = typeof(TActorImplementation);
            Activator = activator;
            CreateStateManager = createStateManager;
            CreateStateProvider = createStateProvider;
            CreateActorService = null;
        }

        public Type InterfaceType { get; private set; }
        public Type ImplementationType { get; private set; }
        public Func<ActorService, ActorId, object> Activator { get; private set; }
        public CreateStateManager CreateStateManager { get; private set; }
        public CreateStateProvider CreateStateProvider { get; private set; }
        public CreateActorService CreateActorService { get; private set; }
    }

    public class MockableActorRegistration : IMockableActorRegistration
    {
        public MockableActorRegistration(
            Type interfaceType, 
            Type implementationType, 
            Func<ActorService, ActorId, object> activator,
            CreateStateManager createStateManager = null,
            CreateStateProvider createStateProvider = null)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            Activator = activator;
            CreateStateManager = createStateManager;
            CreateStateProvider = createStateProvider;
            CreateActorService = null;
        }

        public Type InterfaceType { get; private set; }
        public Type ImplementationType { get; private set; }
        public Func<ActorService, ActorId, object> Activator { get; private set; }
        public CreateStateManager CreateStateManager { get; private set; }
        public CreateStateProvider CreateStateProvider { get; }
        public CreateActorService CreateActorService { get; }
    }

    public class MockableActorRegistration<TActorService> : IMockableActorRegistration
        where TActorService : ActorService
    {
        public MockableActorRegistration(
            Type interfaceType, 
            Type implementationType, 
            CreateActorService<TActorService> createActorService, 
            Func<TActorService, ActorId, object> activator,
            CreateStateManager createStateManager = null,
            CreateStateProvider createStateProvider = null)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            CreateActorService = (CreateActorService) ((
                    StatefulServiceContext context,
                    ActorTypeInformation actorTypeInformation,
                    IActorStateProvider actorStateProvider,
                    Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory) =>
                        createActorService(context, actorTypeInformation, actorStateProvider, stateManagerFactory));
            Activator = (Func<ActorService, ActorId, object>)((service, actorId) => activator((TActorService)service, actorId));
            CreateStateManager = createStateManager;
            CreateStateProvider = createStateProvider;
        }

        public Type InterfaceType { get; private set; }
        public Type ImplementationType { get; private set; }
        public Func<ActorService, ActorId, object> Activator { get; private set; }
        public CreateStateManager CreateStateManager { get; private set; }
        public CreateStateProvider CreateStateProvider { get; }
        public CreateActorService CreateActorService { get; }
    }
}