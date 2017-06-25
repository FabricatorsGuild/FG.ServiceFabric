using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Data;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
    public class ComplexActorService : Actors.Runtime.ActorService, IActorService
    {
        public new IExternalActorStateProvider StateProvider =>  (IExternalActorStateProvider) base.StateProvider;

        public ComplexActorService(
            StatefulServiceContext context, 
            ActorTypeInformation actorTypeInfo, 
            Func<ActorService, ActorId, Actors.Runtime.ActorBase> actorFactory = null, 
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null, 
            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) : 
            base(context, actorTypeInfo, actorFactory, stateManagerFactory, new ComplexFileStore(actorTypeInfo), settings)
        {
        }
        
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            foreach (var serviceReplicaListener in base.CreateServiceReplicaListeners())
            {
                yield return serviceReplicaListener;
            }
        }
    }

    public class ComplexFileStore : FileStore
    {
        public ComplexFileStore(ActorTypeInformation actorTypeInfor, IActorStateProvider stateProvider = null) : base(actorTypeInfor, stateProvider)
        {
        }

        public override async Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            //todo: either here or in Actor.OnActivateAsync and based on some condition?
            await RestoreExternalState<ComplexType>(actorId, "complexType", cancellationToken);
            await base.ActorActivatedAsync(actorId, cancellationToken);
        }
    }
}