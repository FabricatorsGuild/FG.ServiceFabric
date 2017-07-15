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
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
			MockServiceDefinition serviceDefinition = null,
			Uri serviceUri = null)
        {
	        ServiceUri = serviceUri;
            InterfaceType = typeof(TActorInterface);
            ImplementationType = typeof(TActorImplementation);
            Activator = activator;
            CreateActorStateManager = createActorStateManager;
            CreateActorStateProvider = createActorStateProvider;
            CreateActorService = null;
	        ServiceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
        }

        public Type InterfaceType { get; private set; }
        public Type ImplementationType { get; private set; }
        public Func<ActorService, ActorId, object> Activator { get; private set; }
        public CreateActorStateManager CreateActorStateManager { get; private set; }
        public CreateActorStateProvider CreateActorStateProvider { get; private set; }
        public CreateActorService CreateActorService { get; private set; }
	    public MockServiceDefinition ServiceDefinition { get; set; }
	    public Uri ServiceUri { get; set; }
    }

    public class MockableActorRegistration : IMockableActorRegistration
    {
        public MockableActorRegistration(
            Type interfaceType, 
            Type implementationType, 
            Func<ActorService, ActorId, object> activator,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
			MockServiceDefinition serviceDefinition = null,
			Uri serviceUri = null)
        {
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            Activator = activator;
            CreateActorStateManager = createActorStateManager;
            CreateActorStateProvider = createActorStateProvider;
	        ServiceUri = serviceUri;
	        CreateActorService = null;
	        ServiceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
        }

        public Type InterfaceType { get; private set; }
        public Type ImplementationType { get; private set; }
        public Func<ActorService, ActorId, object> Activator { get; private set; }
        public CreateActorStateManager CreateActorStateManager { get; private set; }
        public CreateActorStateProvider CreateActorStateProvider { get; }
        public CreateActorService CreateActorService { get; }
	    public MockServiceDefinition ServiceDefinition { get; set; }
	    public Uri ServiceUri { get; set; }
    }

    public class MockableActorRegistration<TActorService> : IMockableActorRegistration
        where TActorService : ActorService
    {
        public MockableActorRegistration(
            Type interfaceType, 
            Type implementationType, 
            CreateActorService<TActorService> createActorService, 
            Func<TActorService, ActorId, object> activator,
            CreateActorStateManager createActorStateManager = null,
            CreateActorStateProvider createActorStateProvider = null,
			MockServiceDefinition serviceDefinition = null,
			Uri serviceUri = null)
        {
	        serviceDefinition = serviceDefinition ?? MockServiceDefinition.Default;
            InterfaceType = interfaceType;
            ImplementationType = implementationType;
            CreateActorService = (CreateActorService) ((
                    StatefulServiceContext context,
                    ActorTypeInformation actorTypeInformation,
                    IActorStateProvider actorStateProvider,
                    Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory) =>
                        createActorService(context, actorTypeInformation, actorStateProvider, stateManagerFactory));
            Activator = (Func<ActorService, ActorId, object>)((service, actorId) => activator((TActorService)service, actorId));
            CreateActorStateManager = createActorStateManager;
            CreateActorStateProvider = createActorStateProvider;
	        ServiceDefinition = serviceDefinition;
	        ServiceUri = serviceUri;
        }

        public Type InterfaceType { get; private set; }
        public Type ImplementationType { get; private set; }
        public Func<ActorService, ActorId, object> Activator { get; private set; }
        public CreateActorStateManager CreateActorStateManager { get; private set; }
        public CreateActorStateProvider CreateActorStateProvider { get; }
        public CreateActorService CreateActorService { get; }
		public MockServiceDefinition ServiceDefinition { get; set; }
	    public Uri ServiceUri { get; set; }
    }
}