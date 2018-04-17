using System;
using System.Fabric;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    using FG.ServiceFabric.Testing.Mocks.Services.Runtime;

    internal interface IMockableActorRegistration
    {
        bool IsSimple { get; }

        SimpleActorRegistrationConfiguration SimpleActorConfiguration { get; }

        Type InterfaceType { get; }
        Type ImplementationType { get; }
        CreateActorService CreateActorService { get; }
        Func<ActorService, ActorId, object> Activator { get; }
        CreateActorStateManager CreateActorStateManager { get; }
        CreateActorStateProvider CreateActorStateProvider { get; }
        IMockableServiceRegistration ServiceRegistration { get; set; }
    }

    public struct SimpleActorRegistrationConfiguration
    {
        public Func<MockActorContainer, IActorService> ActorServiceFactory { get; set; }

        public Func<IActorService, ActorId, IActor> ActorFactory { get; set; }
    }

    public delegate IActorStateProvider CreateActorStateProvider(StatefulServiceContext context,
        ActorTypeInformation actorTypeInformation);

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