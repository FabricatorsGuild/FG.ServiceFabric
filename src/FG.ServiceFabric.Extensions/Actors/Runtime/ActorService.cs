using System;
using System.Collections.Generic;
using System.Fabric;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class ActorService : Microsoft.ServiceFabric.Actors.Runtime.ActorService
    {
        private IServiceProxyFactory _serviceProxyFactory;
        private IActorProxyFactory _actorProxyFactory;
        private ApplicationUriBuilder _applicationUriBuilder;

        public ActorService(
            StatefulServiceContext context, 
            ActorTypeInformation actorTypeInfo, 
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorId, ActorBase> actorFactory = null, 
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null, 
            IActorStateProvider stateProvider = null, 
            ActorServiceSettings settings = null,
			IReliableStateManagerReplica reliableStateManagerReplica = null) : 
            base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
	        StateManager = reliableStateManagerReplica ??
	                       (IReliableStateManagerReplica) new ReliableStateManager(context, (ReliableStateManagerConfiguration) null);
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return base.CreateServiceReplicaListeners();
        }


	    public IReliableStateManager StateManager { get; private set; }

        public ApplicationUriBuilder ApplicationUriBuilder => _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

        public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ?? (_actorProxyFactory = new ActorProxyFactory());

        public IServiceProxyFactory ServiceProxyFactory => _serviceProxyFactory ?? (_serviceProxyFactory = new ServiceProxyFactory());


    }
}