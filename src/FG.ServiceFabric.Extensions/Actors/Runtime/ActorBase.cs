using System;
using System.Threading.Tasks;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Actors.Runtime
{
	public abstract partial class ActorBase : Microsoft.ServiceFabric.Actors.Runtime.Actor
	{
		private Func<IActorClientLogger> _actorClientLoggerFactory;
		private Func<IActorProxyFactory> _actorProxyFactoryFactory;
		private ApplicationUriBuilder _applicationUriBuilder;
		private Func<IServiceClientLogger> _serviceClientLoggerFactory;
		private Func<IServiceProxyFactory> _serviceProxyFactoryFactory;

		protected ActorBase(
			Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService,
			ActorId actorId) : base(actorService, actorId)
		{
			_applicationUriBuilder = new ApplicationUriBuilder(actorService.Context.CodePackageActivationContext);
			_actorProxyFactoryFactory = () =>
				new FG.ServiceFabric.Actors.Client.ActorProxyFactory(_actorClientLoggerFactory?.Invoke());
			_serviceProxyFactoryFactory = () =>
				new FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory(_serviceClientLoggerFactory?.Invoke());
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

		protected ApplicationUriBuilder ApplicationUriBuilder =>
			_applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

		protected IActorProxyFactory ActorProxyFactory => _actorProxyFactoryFactory();

		protected IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactoryFactory();
	}
}