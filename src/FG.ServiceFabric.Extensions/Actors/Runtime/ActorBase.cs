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
        private readonly Func<IActorClientLogger> _actorClientLoggerFactory;

        private readonly Func<IActorProxyFactory> _actorProxyFactoryFactory;

        private readonly Func<IServiceClientLogger> _serviceClientLoggerFactory;

        private readonly Func<IServiceProxyFactory> _serviceProxyFactoryFactory;

        private ApplicationUriBuilder _applicationUriBuilder;

        protected ActorBase(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            _applicationUriBuilder = new ApplicationUriBuilder(actorService.Context.CodePackageActivationContext);
            _actorProxyFactoryFactory = () => new ActorProxyFactory(_actorClientLoggerFactory?.Invoke());
            _serviceProxyFactoryFactory = () => new ServiceProxyFactory(_serviceClientLoggerFactory?.Invoke());
        }

        protected ActorBase(
            Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService,
            ActorId actorId,
            Func<IActorClientLogger> actorClientLoggerFactory,
            Func<IServiceClientLogger> serviceClientLoggerFactory)
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