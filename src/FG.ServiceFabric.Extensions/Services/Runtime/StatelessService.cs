﻿using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Services.Runtime
{
	public abstract class StatelessService : Microsoft.ServiceFabric.Services.Runtime.StatelessService
	{
		private readonly Func<IActorClientLogger> _actorClientLoggerFactory;
		private IActorProxyFactory _actorProxyFactory;
		private ApplicationUriBuilder _applicationUriBuilder;
		private readonly Func<IServiceClientLogger> _serviceClientLoggerFactory;
		private IServiceProxyFactory _serviceProxyFactory;

		protected StatelessService(StatelessServiceContext serviceContext,
			Func<IActorClientLogger> actorClientLoggerFactory = null,
			Func<IServiceClientLogger> serviceClientLoggerFactory = null) : base(serviceContext)
		{
			_actorClientLoggerFactory = actorClientLoggerFactory;
			_serviceClientLoggerFactory = serviceClientLoggerFactory;
			_applicationUriBuilder = new ApplicationUriBuilder(this.Context.CodePackageActivationContext);
		}

		public ApplicationUriBuilder ApplicationUriBuilder =>
			_applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

		public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ?? (_actorProxyFactory =
			                                               new FG.ServiceFabric.Actors.Client.ActorProxyFactory(
				                                               _actorClientLoggerFactory?.Invoke()));

		public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory ?? (_serviceProxyFactory =
			                                                   new FG.ServiceFabric.Services.Remoting.Runtime.Client.
				                                                   ServiceProxyFactory(_serviceClientLoggerFactory?.Invoke()));
	}
}