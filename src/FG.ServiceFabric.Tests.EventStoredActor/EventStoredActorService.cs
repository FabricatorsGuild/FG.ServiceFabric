using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using ActorBase = FG.ServiceFabric.Actors.Runtime.ActorBase;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;
using IEventStoredActorService = FG.ServiceFabric.Tests.EventStoredActor.Interfaces.IEventStoredActorService;

namespace FG.ServiceFabric.Tests.EventStoredActor
{
    internal class EventStoredActorService
        : EventStoredActorService<Domain, TheEventStream>, IEventStoredActorService
    {
        public EventStoredActorService(
            StatefulServiceContext context,
            ActorTypeInformation actorTypeInfo,
            Func<ActorService, ActorId, ActorBase> actorFactory = null,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager>
                stateManagerFactory = null,
            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) :
            base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
        }

        public Task<ReadModel> GetAsync(Guid aggregateRootId)
        {
            using (var generator = new ReadModelGenerator(StateProviderEventStreamReader))
            {
                return generator.GenerateAsync(aggregateRootId, CancellationToken.None);
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            foreach (var serviceReplicaListener in base.CreateServiceReplicaListeners())
                yield return serviceReplicaListener;
        }
    }
}