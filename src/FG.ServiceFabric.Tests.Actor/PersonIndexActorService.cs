//using System;
//using System.Collections.Generic;
//using System.Fabric;
//using Microsoft.ServiceFabric.Actors;
//using Microsoft.ServiceFabric.Actors.Runtime;
//using Microsoft.ServiceFabric.Services.Communication.Runtime;
//using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

//namespace FG.ServiceFabric.Tests.Actor
//{
//    internal class PersonIndexActorService : FG.ServiceFabric.Actors.Runtime.ActorService, IActorService
//    {
//        public PersonIndexActorService(
//            StatefulServiceContext context,
//            ActorTypeInformation actorTypeInfo,
//            Func<ActorService, ActorId, Actors.Runtime.ActorBase> actorFactory = null,
//            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager>
//                stateManagerFactory = null,
//            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) :
//            base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
//        {
//        }
        
//        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
//        {
//            foreach (var serviceReplicaListener in base.CreateServiceReplicaListeners())
//            {
//                yield return serviceReplicaListener;
//            }
//        }
//    }
//}