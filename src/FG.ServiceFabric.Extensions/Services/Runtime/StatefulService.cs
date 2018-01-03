using System;
using System.Fabric;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using ActorProxyFactory = FG.ServiceFabric.Actors.Client.ActorProxyFactory;
using ServiceProxyFactory = FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory;

namespace FG.ServiceFabric.Services.Runtime
{
    public abstract class StatefulService : Microsoft.ServiceFabric.Services.Runtime.StatefulService, IService
    {
        private readonly Func<IActorClientLogger> _actorClientLoggerFactory;

        private readonly Func<IServiceClientLogger> _serviceClientLoggerFactory;

        private IActorProxyFactory _actorProxyFactory;

        private ApplicationUriBuilder _applicationUriBuilder;

        private IServiceProxyFactory _serviceProxyFactory;

        protected StatefulService(
            StatefulServiceContext serviceContext,
            Func<IActorClientLogger> actorClientLoggerFactory = null,
            Func<IServiceClientLogger> serviceClientLoggerFactory = null)
            : base(serviceContext)
        {
            _actorClientLoggerFactory = actorClientLoggerFactory;
            _serviceClientLoggerFactory = serviceClientLoggerFactory;
            _applicationUriBuilder = new ApplicationUriBuilder(Context.CodePackageActivationContext);
        }

        protected StatefulService(
            StatefulServiceContext serviceContext,
            IReliableStateManagerReplica2 reliableStateManagerReplica,
            Func<IActorClientLogger> actorClientLoggerFactory = null,
            Func<IServiceClientLogger> serviceClientLoggerFactory = null)
            : base(serviceContext, reliableStateManagerReplica)
        {
            _actorClientLoggerFactory = actorClientLoggerFactory;
            _serviceClientLoggerFactory = serviceClientLoggerFactory;
            _applicationUriBuilder = new ApplicationUriBuilder(Context.CodePackageActivationContext);
        }

        public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ??
                                                       (_actorProxyFactory =
                                                           new ActorProxyFactory(_actorClientLoggerFactory?.Invoke()));

        public ApplicationUriBuilder ApplicationUriBuilder =>
            _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

        public IServiceProxyFactory ServiceProxyFactory =>
            _serviceProxyFactory ??
            (_serviceProxyFactory = new ServiceProxyFactory(_serviceClientLoggerFactory?.Invoke()));

        [Obsolete("Dont use this, use IStateSessionManager API instead", true)]
        public new IReliableStateManager StateManager => base.StateManager;
    }
}