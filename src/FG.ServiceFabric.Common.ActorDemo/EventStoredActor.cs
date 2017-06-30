using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;
using IAggregateRootEvent = FG.ServiceFabric.CQRS.IAggregateRootEvent;

namespace FG.ServiceFabric.Tests.Actor
{
    [StatePersistence(StatePersistence.Persisted)]
    public class EventStoredActor : 
        EventStoredActor<EventStoredActor.MyAggregateRoot, EventStoredActor.MyEventStream>,
        IEventStoredActor, 
        IHandleDomainEvent<EventStoredActor.MyDomainEvent>, 
        IHandleDomainEvent<EventStoredActor.MySecondDomainEvent>
    {
        public EventStoredActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override async Task OnActivateAsync()
        {
            //here or in state provider onactoractivated.
            //todo:restore external state if no state exist in state provider
            //todo:reminders?
            //todo:check for unpublished?
            //todo:kill actors without state anywhere.
            await GetAndSetDomainAsync();
        }

        public Task CreateAsync(Guid aggregateRootId, string value)
        {
            DomainState.MyDomainAction(aggregateRootId, value);
            return Task.FromResult(true);
        }
        public Task UpdateAsync(Guid aggregateRootId, string value)
        {
            DomainState.MySecondDomainAction(aggregateRootId, value);
            return Task.FromResult(true);
        }

        public async Task Handle(MyDomainEvent domainEvent)
        {
            //await StoreDomainEventAsync(domainEvent);
        }

        public async Task Handle(MySecondDomainEvent domainEvent)
        {
            //await StoreDomainEventAsync(domainEvent);
        }

        #region Sample domain

        public interface IMyEventInterface : IAggregateRootEvent
        {
        }

        public interface IMyDomainEvent : IMyEventInterface
        {
            string MyProp { get; }
        }

        [DataContract]
        public class MyDomainEvent : AggregateRootEventBase, IMyDomainEvent, IAggregateRootCreatedEvent
        {
            [DataMember]
            public string MyProp { get; set; }
        }

        [DataContract]
        public class MySecondDomainEvent : AggregateRootEventBase, IMyDomainEvent
        {
            [DataMember]
            public string MyProp { get; set; }
        }

        public class MyAggregateRoot : AggregateRoot<IMyEventInterface>
        {
            public MyAggregateRoot()
            {
                RegisterEventAppliers()
                    .For<IMyDomainEvent>(e => this.MyProp = e.MyProp);
            }

            public string MyProp { get; set; }

            public void MyDomainAction(Guid aggregateRootId, string arg)
            {
                RaiseEvent(new MyDomainEvent {AggregateRootId = aggregateRootId, MyProp = arg});
            }

            public void MySecondDomainAction(Guid aggregateRootId, string arg)
            {
                //try raise 2 events.
                RaiseEvent(new MySecondDomainEvent { MyProp = arg + "1" });
                RaiseEvent(new MySecondDomainEvent { MyProp = arg + "2" });
            }
        }

        [DataContract]
        [KnownType(typeof(MyDomainEvent))]
        [KnownType(typeof(MySecondDomainEvent))]
        public class MyEventStream : EventStreamBase
        {
        }

        #endregion
    }
}
