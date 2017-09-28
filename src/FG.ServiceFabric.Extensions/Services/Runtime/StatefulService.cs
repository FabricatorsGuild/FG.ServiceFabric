using System;
using System.Fabric;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Services.Runtime
{
    public abstract class StatefulService : Microsoft.ServiceFabric.Services.Runtime.StatefulService, IService
    {
        private IServiceProxyFactory _serviceProxyFactory;
        private ApplicationUriBuilder _applicationUriBuilder;
        private IActorProxyFactory _actorProxyFactory;
	    private Func<IActorClientLogger> _actorClientLoggerFactory;
	    private Func<IServiceClientLogger> _serviceClientLoggerFactory;

		protected StatefulService(StatefulServiceContext serviceContext, 
			Func<IActorClientLogger> actorClientLoggerFactory = null,
			Func<IServiceClientLogger> serviceClientLoggerFactory = null) : base(serviceContext)
        {
			_actorClientLoggerFactory = actorClientLoggerFactory;
			_serviceClientLoggerFactory = serviceClientLoggerFactory;
			_applicationUriBuilder = new ApplicationUriBuilder(this.Context.CodePackageActivationContext);
        }

	    protected StatefulService(StatefulServiceContext serviceContext, 
			IReliableStateManagerReplica2 reliableStateManagerReplica, 
			Func<IActorClientLogger> actorClientLoggerFactory = null, 
			Func<IServiceClientLogger> serviceClientLoggerFactory = null) : base(serviceContext, reliableStateManagerReplica)
	    {
		    _actorClientLoggerFactory = actorClientLoggerFactory;
		    _serviceClientLoggerFactory = serviceClientLoggerFactory;
		    _applicationUriBuilder = new ApplicationUriBuilder(this.Context.CodePackageActivationContext);
	    }


        public ApplicationUriBuilder ApplicationUriBuilder => _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

        public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ?? (_actorProxyFactory = new FG.ServiceFabric.Actors.Client.ActorProxyFactory(_actorClientLoggerFactory?.Invoke()));

        public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory ?? (_serviceProxyFactory = new FG.ServiceFabric.Services.Remoting.Runtime.Client.ServiceProxyFactory(_serviceClientLoggerFactory?.Invoke()));                
    }
}