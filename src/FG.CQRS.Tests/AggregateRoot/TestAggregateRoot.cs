using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Components.DictionaryAdapter;

// ReSharper disable InconsistentNaming

namespace FG.CQRS.Tests.AggregateRoot
{
    #region Aggregate root under test

    public class BaseEvent : IAggregateRootEvent
    {
        public BaseEvent()
        {
            EventId = Guid.NewGuid();
            UtcTimeStamp = DateTime.UtcNow;
        }

        public Guid EventId { get; }
        public DateTime UtcTimeStamp { get; set; }
        public Guid AggregateRootId { get; set; }
        public int Version { get; set; }
    }

    public class MaliciousEvent : BaseEvent, ITestEventBase
    {
    }

    public class UnhandledEvent : BaseEvent, ITestEventBase
    {
    }

    public class TestCreatedEventCreatingAllChilrenInOneGoEvent : BaseEvent, ITestCreatedEvent, ITestEntityL1AddedEvent,
        ITestEntityL2AddedEvent
    {
        public int Entityl1Id { get; set; }
        public string Name { get; set; }
        public int Entityl2Id { get; set; }
        public int Value { get; set; }
    }

    public class TestAggregateRoot : AggregateRoot<ITestEventBase>
    {
        public TestAggregateRoot()
        {
            RegisterEventAppliers()
                .For<ITestCreatedEvent>(e => { })
                .For<ITestEntityL1AddedEvent>(e => EntityL1S.Add(new TestEntityL1(this, e.Entityl1Id)))
                .For<ITestEntityL1EventBase>(e => EntityL1S.Single(x => x.Entityl1Id == e.Entityl1Id).ApplyEvent(e));
        }

        public List<TestEntityL1> EntityL1S { get; } = new EditableList<TestEntityL1>();

        public void Create(Guid aggregateRootId)
        {
            RaiseEvent(new TestCreatedEvent {AggregateRootId = aggregateRootId});
        }

        public void AddEntity(string name)
        {
            RaiseEvent(new TestEntityL1AddedEvent(1) {Name = name});
        }

        public void ForTestForceRaiseAnyEvent(ITestEventBase evt)
        {
            RaiseEvent(evt);
        }

        public class TestEntityL1 : Entity<TestAggregateRoot, ITestEntityL1EventBase>
        {
            public TestEntityL1(TestAggregateRoot aggregateRoot, int entityl1Id) : base(aggregateRoot)
            {
                Entityl1Id = entityl1Id;
                RegisterEventAppliers()
                    .For<ITestEntityL1AddedEvent>(e => Name = e.Name)
                    .For<ITestEntityL2AddedEvent>(e => EntityL2S.Add(new TestEntityL2(aggregateRoot, e.Entityl2Id)))
                    .For<ITestEntityL2EventBase>(e =>
                        EntityL2S.Single(x => x.Entityl2Id == e.Entityl2Id).ApplyEvent(e));
            }

            public int Entityl1Id { get; }
            public List<TestEntityL2> EntityL2S { get; set; } = new EditableList<TestEntityL2>();
            public string Name { get; private set; }

            public void AddEntity(int value)
            {
                RaiseEvent(new TestEntityL2AddedEvent(Entityl1Id, 1) {Value = value});
            }

            public class TestEntityL2 : Entity<TestAggregateRoot, ITestEntityL2EventBase>
            {
                public TestEntityL2(TestAggregateRoot aggregateRoot, int entityl2Id) : base(aggregateRoot)
                {
                    Entityl2Id = entityl2Id;
                    RegisterEventAppliers()
                        .For<ITestEntityL2AddedEvent>(e => Value = e.Value);
                }

                public int Entityl2Id { get; }
                public int Value { get; private set; }
            }
        }
    }

    public class TestEventStream : IDomainEventStream
    {
        public TestEventStream()
        {
            DomainEvents = new IDomainEvent[] { };
        }

        public IDomainEvent[] DomainEvents { get; set; }

        public void Append(IDomainEvent domainEvent)
        {
            DomainEvents = DomainEvents.Union(new[] {domainEvent}).ToArray();
        }
    }

    public interface ITestEventBase : IAggregateRootEvent
    {
    }

    public interface ITestEntityL1EventBase : ITestEventBase
    {
        int Entityl1Id { get; set; }
    }

    public interface ITestEntityL2EventBase : ITestEntityL1EventBase
    {
        int Entityl2Id { get; set; }
    }

    public interface ITestCreatedEvent : ITestEventBase, IAggregateRootCreatedEvent
    {
    }

    public class TestCreatedEvent : BaseEvent, ITestCreatedEvent
    {
    }

    public interface ITestEntityL1AddedEvent : ITestEntityL1EventBase
    {
        string Name { get; set; }
    }

    public class TestEntityL1AddedEvent : BaseEvent, ITestEntityL1AddedEvent
    {
        public TestEntityL1AddedEvent(int entityl1Id)
        {
            Entityl1Id = entityl1Id;
        }

        public int Entityl1Id { get; set; }
        public string Name { get; set; }
    }

    public interface ITestEntityL2AddedEvent : ITestEntityL2EventBase
    {
        int Value { get; set; }
    }

    public class TestEntityL2AddedEvent : BaseEvent, ITestEntityL2AddedEvent
    {
        public TestEntityL2AddedEvent(int entityl1Id, int entityl2Id)
        {
            Entityl1Id = entityl1Id;
            Entityl2Id = entityl2Id;
        }

        public int Entityl1Id { get; set; }
        public int Entityl2Id { get; set; }
        public int Value { get; set; }
    }

    #endregion
}