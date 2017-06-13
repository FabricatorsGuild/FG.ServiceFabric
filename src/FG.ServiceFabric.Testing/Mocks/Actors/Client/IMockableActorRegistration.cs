using System;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    public interface IMockableActorRegistration
    {
        Type InterfaceType { get; }
        Type ImplementationType { get; }
        CreateActorService CreateActorService { get; }
        Func<ActorService, ActorId, object> Activator { get; }
        CreateStateManager CreateStateManager { get; }
        CreateStateProvider CreateStateProvider { get; }        
    }

    public delegate IActorStateProvider CreateStateProvider();

    public delegate IActorStateManager CreateStateManager(ActorBase actor, IActorStateProvider stateProvider);

    public delegate ActorService CreateActorService(
        StatefulServiceContext context,
        ActorTypeInformation actorTypeInformation,
        IActorStateProvider actorStateProvider,
        Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory);

    public delegate TActorService CreateActorService<out TActorService>(
        StatefulServiceContext context,
        ActorTypeInformation actorTypeInformation,
        IActorStateProvider actorStateProvider,
        Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
        where TActorService : ActorService;
}