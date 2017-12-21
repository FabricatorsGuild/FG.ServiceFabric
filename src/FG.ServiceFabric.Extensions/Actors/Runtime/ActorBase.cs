namespace FG.ServiceFabric.Actors.Runtime
{
    using System;

    using FG.ServiceFabric.Diagnostics;
    using FG.ServiceFabric.Services.Runtime;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    using ActorProxyFactory = FG.ServiceFabric.Actors.Client.ActorProxyFactory;
    using ServiceProxyFactory = FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory;

    public abstract class ActorBase : Actor
    {
        private readonly Func<IActorClientLogger> _actorClientLoggerFactory;

        private readonly Func<IActorProxyFactory> _actorProxyFactoryFactory;

        private ApplicationUriBuilder _applicationUriBuilder;

        private readonly Func<IServiceClientLogger> _serviceClientLoggerFactory;

        private readonly Func<IServiceProxyFactory> _serviceProxyFactoryFactory;

        protected ActorBase(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            this._applicationUriBuilder = new ApplicationUriBuilder(actorService.Context.CodePackageActivationContext);
            this._actorProxyFactoryFactory = () => new ActorProxyFactory(this._actorClientLoggerFactory?.Invoke());
            this._serviceProxyFactoryFactory = () => new ServiceProxyFactory(this._serviceClientLoggerFactory?.Invoke());
        }

        protected ActorBase(
            Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService,
            ActorId actorId,
            Func<IActorClientLogger> actorClientLoggerFactory,
            Func<IServiceClientLogger> serviceClientLoggerFactory)
            : this(actorService, actorId)
        {
            this._actorClientLoggerFactory = actorClientLoggerFactory;
            this._serviceClientLoggerFactory = serviceClientLoggerFactory;
        }

        protected IActorProxyFactory ActorProxyFactory => this._actorProxyFactoryFactory();

        protected ApplicationUriBuilder ApplicationUriBuilder => this._applicationUriBuilder ?? (this._applicationUriBuilder = new ApplicationUriBuilder());

        protected IServiceProxyFactory ServiceProxyFactory => this._serviceProxyFactoryFactory();
    }
}