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
    public class EventStoredActor : EventStoredActor<MyEventStream>, IEventStoredActor, 
        IHandleDomainEvent<MyDomainEvent>,
        IHandleDomainEvent<MySecondDomainEvent>
    {
        private MyAggregateRoot _aggregate;

        public EventStoredActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override async Task OnActivateAsync()
        {
            _aggregate = await EventStoreSession.GetAsync<MyAggregateRoot>();
            await base.OnActivateAsync();
        }

        public async Task CreateAsync(MyCommand command)
        {
            _aggregate.MyDomainAction(command.AggretateRootId, command.Value);
            await EventStoreSession.SaveChanges();
        }
        public async Task UpdateAsync(MyCommand command)
        {
            _aggregate.MySecondDomainAction(command.Value);
            await EventStoreSession.SaveChanges();
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
