using System;
using System.Collections.Generic;
using System.Fabric;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Tests.Actor.Domain;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FG.ServiceFabric.Tests.Actor.Query;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.Actor
{
    internal class PersonEventStoredActorService 
        : EventStoredActorService<Person,PersonEventStream>, IPersonEventStoredActorService
    {
        public PersonEventStoredActorService(
            StatefulServiceContext context,
            ActorTypeInformation actorTypeInfo,
            Func<ActorService, ActorId, Actors.Runtime.ActorBase> actorFactory = null,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager>
                stateManagerFactory = null,
            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) :
            base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
        }
        
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            foreach (var serviceReplicaListener in base.CreateServiceReplicaListeners())
            {
                yield return serviceReplicaListener;
            }
        }

        public Task<PersonReadModel> GetAsync(Guid aggregateRootId)
        {
            using (var generator = new ReadModelGenerator(StateProviderEventStreamReader))
            {
                return generator.GenerateAsync(aggregateRootId, CancellationToken.None);
            }
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            //var key = $"Actor_{actorId}_{stateName}

            return base.RunAsync(cancellationToken);
        }

        internal async Task EnqueuePending(ActorId actorId, string stateName)
        {
            using (var tx = StateManager.CreateTransaction())
            {
                var queue = await StateManager.GetOrAddAsync<IReliableQueue<Something>>(tx, "outboundQueue");
                await queue.EnqueueAsync(tx, new Something());
            }
        }
        

        [DataContract]
        internal class Something
        {
        }
    }
}