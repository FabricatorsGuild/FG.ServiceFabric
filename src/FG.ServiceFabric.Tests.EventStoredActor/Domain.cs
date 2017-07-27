using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FG.CQRS;
using FG.CQRS.Exceptions;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;

namespace FG.ServiceFabric.Tests.EventStoredActor
{
    #region Domain event interfaces
    public interface IRootEvent : IAggregateRootEvent { }

    public interface ISomePropertyUpdated : IRootEvent
    {
        string SomeProperty { get; }
    }
    
    public interface IChildEvent : IRootEvent
    {
        int ChildId { get; }
    }

    public interface IChildAdded : IChildEvent
    {
    }
    
    public interface IChildPropertyUpdated : IChildEvent
    {
        string ChildProperty { get; set; }
    }

    #endregion

    #region Domain events
    [DataContract]
    public class CreatedEvent : AggregateRootEventBase, ISomePropertyUpdated, IAggregateRootCreatedEvent
    {
        [DataMember]
        public string SomeProperty { get; set; }
    }
    
    [DataContract]
    public class ChildAddedEvent : AggregateRootEventBase, IChildAdded, IChildPropertyUpdated
    {
        [DataMember]
        public int ChildId { get; set; }

        [DataMember]
        public string ChildProperty { get; set; }

    }
    #endregion

    #region Event stream (service fabric specific)

    [DataContract]
    [KnownType(typeof(CreatedEvent))]
    [KnownType(typeof(ChildAddedEvent))]
    public class TheEventStream : EventStreamStateBase
    {
    }

    #endregion

    #region Aggregate
    public class Domain : AggregateRoot<IRootEvent>
    {
        public Domain()
        {
            RegisterEventAppliers()
                .For<ISomePropertyUpdated>(e => SomeProperty = e.SomeProperty)
                .For<IChildAdded>(e =>
                {
                    _children.Add(new Child(this, e.ChildId));
                    _maxChildId = Math.Max(_maxChildId, e.ChildId);
                })
                .For<IChildEvent>(e => _children.Single(c => c.ChildId == e.ChildId).ApplyEvent(e))
                ;
        }

        public override void AssertInvariantsAreMet()
        {
            if(string.IsNullOrWhiteSpace(SomeProperty))
                throw new InvariantsNotMetException(nameof(SomeProperty));

            base.AssertInvariantsAreMet();
        }
        
        public string SomeProperty { get; private set; }
        private readonly List<Child> _children = new List<Child>();
        public IReadOnlyCollection<Child> Children => _children.AsReadOnly();
        private int _maxChildId = 0;

        public void Create(Guid aggregateRootId, string someProperty)
        {
            if (string.IsNullOrWhiteSpace(someProperty))
                throw new Exception("Invalid first name");

            RaiseEvent(new CreatedEvent { AggregateRootId = aggregateRootId, SomeProperty = someProperty });
        }
        
        public int AddChild(Guid commandId)
        {
            var childId = _maxChildId + 1;
            RaiseEvent(new ChildAddedEvent { ChildId = childId });
            return childId;
        }

        public class Child : Entity<Domain, IChildEvent>
        {
            public Child(Domain aggregateRoot, int id) : base(aggregateRoot)
            {
                ChildId = id;

                RegisterEventAppliers().For<IChildPropertyUpdated>(e => ChildProperty = e.ChildProperty);
            }

            public int ChildId { get; }

            public string ChildProperty { get; private set; }
        }
    }
    #endregion

    #region Read Models

    public class ReadModelGenerator : AggregateRootReadModelGenerator<TheEventStream, IRootEvent, ReadModel>
    {
        public ReadModelGenerator(IEventStreamReader<TheEventStream> eventStreamReader) : base(eventStreamReader)
        {
            RegisterEventAppliers()
                .For<ISomePropertyUpdated>(e => ReadModel.SomeProperty = e.SomeProperty)
                ;
        }
    }

    #endregion
}
