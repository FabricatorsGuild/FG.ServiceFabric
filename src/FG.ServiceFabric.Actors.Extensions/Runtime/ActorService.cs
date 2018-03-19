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
        private IActorProxyFactory _actorProxyFactory;
        private ApplicationUriBuilder _applicationUriBuilder;
        private IServiceProxyFactory _serviceProxyFactory;

        public ActorService(
            StatefulServiceContext context,
            ActorTypeInformation actorTypeInfo,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorId, ActorBase> actorFactory = null,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager>
                stateManagerFactory =
                null,
            IActorStateProvider stateProvider = null,
            ActorServiceSettings settings = null,
            IReliableStateManagerReplica reliableStateManagerReplica = null) :
            base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            StateManager = reliableStateManagerReplica ??
                           new ReliableStateManager(context,
                               null);
        }


        public IReliableStateManager StateManager { get; }

        public ApplicationUriBuilder ApplicationUriBuilder =>
            _applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

        public IActorProxyFactory ActorProxyFactory =>
            _actorProxyFactory ?? (_actorProxyFactory = new ActorProxyFactory());

        public IServiceProxyFactory ServiceProxyFactory =>
            _serviceProxyFactory ?? (_serviceProxyFactory = new ServiceProxyFactory());

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return base.CreateServiceReplicaListeners();
        }
    }
}