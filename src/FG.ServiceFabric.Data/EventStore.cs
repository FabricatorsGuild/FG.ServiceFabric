using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Data
{
    public interface IEventStore<TEvent> : IActorStateProvider where TEvent: IEvent
    {
        Task<IEnumerable<TEvent>> GetHistory(ActorId actorId, CancellationToken cancellationToken = new CancellationToken());
    }

    public interface IUnitOfWorkParticipant
    {

    }

    public interface IEvent
    {
        Guid EventId { get; }
    }
    
    public partial class EventStore<TEvent> : WrappedActorStateProvider, IEventStore<TEvent> where TEvent : IEvent
    {
        public EventStore(ActorTypeInformation actorTypeInfo, IActorStateProvider stateProvider = null) : base(actorTypeInfo, stateProvider)
        {
        }

        private const string EventIndexKey = "fg__eventStreamIndex";

        public override async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //todo:transactional unit of work
            //Publish all changes within unit of work.
            //Store unit of work
            PublishEvents(stateChanges);

            //var eventIndex = await BuildEventIndexAsync(actorId, stateChanges, cancellationToken);
            await base.SaveStateAsync(actorId, stateChanges, cancellationToken);
        }

        private async Task<ActorStateChange> BuildEventIndexAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken)
        {
            var stateNamesOfNewEvents = stateChanges
                .Where(sc => sc.Value is TEvent && sc.ChangeKind == StateChangeKind.Add)
                .Select(sc => sc.StateName)
                .ToArray();

            if (await ContainsStateAsync(actorId, EventIndexKey, cancellationToken))
            {
                var index = await LoadStateAsync<EventIndex>(actorId, EventIndexKey, cancellationToken);
                return new ActorStateChange(EventIndexKey, typeof(EventIndex), index.Append(stateNamesOfNewEvents), StateChangeKind.Update);
            }

            return new ActorStateChange(EventIndexKey, typeof(EventIndex), new EventIndex().Append(stateNamesOfNewEvents), StateChangeKind.Add);
        }
        
        public async Task<IEnumerable<TEvent>> GetHistory(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            var eventIndex = await LoadStateAsync<EventIndex>(actorId, EventIndexKey, cancellationToken);
            var tasks = eventIndex.EventStateNames.Select(esn => LoadStateAsync<TEvent>(actorId, esn, cancellationToken)).ToArray();

            return await Task.WhenAll(tasks);
        }

        [DataContract]
        private class EventIndex
        {
            [DataMember]
            public string[] EventStateNames { get; set; }

            public EventIndex Append(IEnumerable<string> eventStateNames)
            {
                return new EventIndex {EventStateNames = EventStateNames.Union(eventStateNames).ToArray()};
            }
        }

        private void PublishEvents(IEnumerable<ActorStateChange> stateChanges)
        {
            foreach (var actorStateChange in stateChanges)
            {
                if (actorStateChange.Value is TEvent)
                {
                    if (actorStateChange.ChangeKind != StateChangeKind.Add)
                    {
                        throw new Exception(
                            $"Unexpected {nameof(actorStateChange.ChangeKind)}: {actorStateChange.ChangeKind}");
                    }

                    //todo:communicate with a service bus
                    Task.Run(() => new SomeBus().Publish(
                        new
                        {
                            actorStateChange.Type.Name,
                            Type = actorStateChange.Type.FullName,
                            Data = actorStateChange.Value
                        }));
                }
            }
        }
    }
}