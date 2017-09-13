using System;
using System.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    internal interface IMockableActorRegistration
    {
        Type InterfaceType { get; }
        Type ImplementationType { get; }
        CreateActorService CreateActorService { get; }
        Func<ActorService, ActorId, object> Activator { get; }
        CreateActorStateManager CreateActorStateManager { get; }
        CreateActorStateProvider CreateActorStateProvider { get; }
		IMockableServiceRegistration ServiceRegistration { get; set; }
    }

	public delegate IActorStateProvider CreateActorStateProvider(StatefulServiceContext context, ActorTypeInformation actorTypeInformation);

    public delegate IActorStateManager CreateActorStateManager(ActorBase actor, IActorStateProvider stateProvider);

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