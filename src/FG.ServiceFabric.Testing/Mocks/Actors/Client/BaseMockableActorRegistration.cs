namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    using System;

    using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public abstract class BaseMockableActorRegistration : IMockableActorRegistration
    {
        public virtual bool IsSimple => false;

        public SimpleActorRegistrationConfiguration SimpleActorConfiguration { get; protected set; }

        public Type InterfaceType { get; protected set; }

        public Type ImplementationType { get; protected set; }

        public CreateActorService CreateActorService { get; protected set; }

        public Func<ActorService, ActorId, object> Activator { get; protected set; }

        public CreateActorStateManager CreateActorStateManager { get; protected set; }

        public CreateActorStateProvider CreateActorStateProvider { get; protected set; }

        public IMockableServiceRegistration ServiceRegistration { get; set; }
    }
}