﻿namespace FG.ServiceFabric.Services.Runtime
{
    using System;
    using System.Fabric;

    using FG.ServiceFabric.Diagnostics;

    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    using ActorProxyFactory = FG.ServiceFabric.Actors.Client.ActorProxyFactory;
    using ServiceProxyFactory = FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory;

    public abstract class StatelessService : Microsoft.ServiceFabric.Services.Runtime.StatelessService
    {
        private readonly Func<IActorClientLogger> _actorClientLoggerFactory;

        private readonly Func<IServiceClientLogger> _serviceClientLoggerFactory;

        private IActorProxyFactory _actorProxyFactory;

        private ApplicationUriBuilder _applicationUriBuilder;

        private IServiceProxyFactory _serviceProxyFactory;

        protected StatelessService(
            StatelessServiceContext serviceContext,
            Func<IActorClientLogger> actorClientLoggerFactory = null,
            Func<IServiceClientLogger> serviceClientLoggerFactory = null)
            : base(serviceContext)
        {
            this._actorClientLoggerFactory = actorClientLoggerFactory;
            this._serviceClientLoggerFactory = serviceClientLoggerFactory;
            this._applicationUriBuilder = new ApplicationUriBuilder(this.Context.CodePackageActivationContext);
        }

        public IActorProxyFactory ActorProxyFactory => this._actorProxyFactory ?? (this._actorProxyFactory = new ActorProxyFactory(this._actorClientLoggerFactory?.Invoke()));

        public ApplicationUriBuilder ApplicationUriBuilder => this._applicationUriBuilder ?? (this._applicationUriBuilder = new ApplicationUriBuilder());

        public IServiceProxyFactory ServiceProxyFactory =>
            this._serviceProxyFactory ?? (this._serviceProxyFactory = new ServiceProxyFactory(this._serviceClientLoggerFactory?.Invoke()));
    }
}