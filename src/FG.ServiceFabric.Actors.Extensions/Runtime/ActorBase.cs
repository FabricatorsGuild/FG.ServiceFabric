using System;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using ActorProxyFactory = FG.ServiceFabric.Actors.Client.ActorProxyFactory;
using ServiceProxyFactory = FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class ActorBase : Actor
    {
        private readonly Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorBase, IActorClientLogger> _actorClientLoggerFactory;

        private readonly Func<IActorProxyFactory> _actorProxyFactoryFactory;

        private readonly Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorBase, IServiceClientLogger> _serviceClientLoggerFactory;

        private readonly Func<IServiceProxyFactory> _serviceProxyFactoryFactory;

        private ApplicationUriBuilder _applicationUriBuilder;

        protected ActorBase(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            _applicationUriBuilder = new ApplicationUriBuilder(actorService.Context.CodePackageActivationContext);
            _actorProxyFactoryFactory = () => new ActorProxyFactory(_actorClientLoggerFactory?.Invoke(actorService, this));
            _serviceProxyFactoryFactory = () => new ServiceProxyFactory(_serviceClientLoggerFactory?.Invoke(actorService, this));
        }

        protected ActorBase(
            Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService,
            ActorId actorId,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorBase, IActorClientLogger> actorClientLoggerFactory,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorBase, IServiceClientLogger> serviceClientLoggerFactory)
            : this(actorService, actorId)
        {
            _actorClientLoggerFactory = actorClientLoggerFactory;
            _serviceClientLoggerFactory = serviceClientLoggerFactory;
        }

        protected IActorProxyFactory ActorProxyFactory => _actorProxyFactoryFactory();

        protected ApplicationUriBuilder ApplicationUriBuilder =>
            _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

        protected IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactoryFactory();
    }
}