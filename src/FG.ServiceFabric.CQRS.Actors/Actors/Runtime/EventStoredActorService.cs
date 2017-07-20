using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.CQRS;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IEventStoredActorService : IActorService
    {
        Task<IEnumerable<string>> GetAllEventHistoryAsync(Guid aggregateRootId);
    }

    public abstract class EventStoredActorService<TAggregateRoot, TEventStream> : ActorService, IEventStoredActorService
        where TEventStream : IDomainEventStream, new()
        where TAggregateRoot : class, IEventStored, new()
    {
        protected readonly IEventStreamReader<TEventStream> StateProviderEventStreamReader;

        protected EventStoredActorService
        (StatefulServiceContext context,
            ActorTypeInformation actorTypeInfo,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorId, ActorBase> actorFactory = null,
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager>
                stateManagerFactory = null,
            IActorStateProvider stateProvider = null,
            ActorServiceSettings settings = null,
            IReliableStateManagerReplica reliableStateManagerReplica = null,
            IEventStreamReader<TEventStream> eventStreamReader = null)
            : base(
                context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings,
                reliableStateManagerReplica)
        {
            StateProviderEventStreamReader = eventStreamReader ??
                                             new EventStreamReader<TEventStream>(StateProvider,
                                                 EventStoredActor<TAggregateRoot, TEventStream>.EventStreamStateKey);
        }

        public async Task<IEnumerable<string>> GetAllEventHistoryAsync(Guid aggregateRootId)
        {
            var events = await StateProviderEventStreamReader.GetEventStreamAsync(aggregateRootId,
                CancellationToken.None);

            return events.DomainEvents.OfType<IAggregateRootEvent>().Select(
                @event => JsonConvert.SerializeObject(new {Event = @event.GetType().Name, Payload = @event})).ToList();
        }
    }
}