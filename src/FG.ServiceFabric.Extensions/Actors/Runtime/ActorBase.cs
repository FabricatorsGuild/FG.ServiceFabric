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
        private IServiceProxyFactory _serviceProxyFactory;
        private IActorProxyFactory _actorProxyFactory;
        private ApplicationUriBuilder _applicationUriBuilder;
		private Func<IActorClientLogger> _actorClientLoggerFactory;
		private Func<IServiceClientLogger> _serviceClientLoggerFactory;

		protected ActorBase(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
            _applicationUriBuilder = new ApplicationUriBuilder(actorService.Context.CodePackageActivationContext);
            
        }

        public ApplicationUriBuilder ApplicationUriBuilder => _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

		public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ?? (_actorProxyFactory = new FG.ServiceFabric.Actors.Client.ActorProxyFactory(_actorClientLoggerFactory?.Invoke()));

		public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory ?? (_serviceProxyFactory = new FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory(_serviceClientLoggerFactory?.Invoke()));
	}
}