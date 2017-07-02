using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;
using EventStreamBase = FG.ServiceFabric.Actors.Runtime.EventStreamBase;
using IAggregateRootEvent = FG.ServiceFabric.CQRS.IAggregateRootEvent;

namespace FG.ServiceFabric.Tests.Actor
{
    [StatePersistence(StatePersistence.Volatile)]
    public class EventStoredActor : FG.ServiceFabric.Actors.Runtime.EventStoredActor, IEventStoredActor, 
        IHandleDomainEvent<MyDomainEvent>,
        IHandleDomainEvent<MySecondDomainEvent>
    {
        private readonly Func<IEventStoreSession> _eventStoreSessionFactory;
        private IEventStoreSession _eventStoreSession;

        public EventStoredActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            _eventStoreSessionFactory = () => new EventStoreSession<MyEventStream>(this.StateManager, this);
        }

        protected override Task OnActivateAsync()
        {
            _eventStoreSession = _eventStoreSessionFactory();
            return base.OnActivateAsync();
        }

        public async Task CreateAsync(MyCommand command)
        {
            var aggregate = await _eventStoreSession.Get<MyAggregateRoot>();
            aggregate.MyDomainAction(command.AggretateRootId, command.Value);
            await _eventStoreSession.SaveChanges();
        }
        public async Task UpdateAsync(MyCommand command)
        {
            var aggregate = await _eventStoreSession.Get<MyAggregateRoot>();
            aggregate.MySecondDomainAction(command.Value);
            await _eventStoreSession.SaveChanges();
        }

        public Task Handle(MyDomainEvent domainEvent)
        {
            return Task.FromResult(true);
        }

        public Task Handle(MySecondDomainEvent domainEvent)
        {
            return Task.FromResult(true);
        }
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
            RaiseEvent(new MyDomainEvent { AggregateRootId = aggregateRootId, MyProp = arg });
        }

        public void MySecondDomainAction(string arg)
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
