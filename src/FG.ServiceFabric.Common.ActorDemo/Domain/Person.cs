using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.CQRS.Exceptions;
using FG.ServiceFabric.Tests.Actor.Interfaces;

namespace FG.ServiceFabric.Tests.Actor.Domain
{
    #region Domain event interfaces
    public interface IPersonEvent : IAggregateRootEvent { }

    public interface IPersonFirstNameUpdated : IPersonEvent
    {
        string FirstName { get; }
    }

    public interface IMaritalStatusUpdated : IPersonEvent
    {
        MaritalStatus MaritalStatus { get; }
    }

    public interface IChildEvent : IPersonEvent
    {
        int ChildId { get; }
    }

    public interface IChildAdded : IChildEvent
    {
    }
    
    public interface IChildLastNameUpdated : IChildEvent
    {
        string LastName { get; set; }
    }

    #endregion

    #region Domain events
    [DataContract]
    public class PersonRegisteredEvent : AggregateRootEventBase, IPersonFirstNameUpdated, IAggregateRootCreatedEvent
    {
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
    }

    [DataContract]
    public class PersonMarriedEvent : AggregateRootEventBase, IMaritalStatusUpdated
    {
        [DataMember]
        public MaritalStatus MaritalStatus { get; set; }

        [DataMember]
        public string LastName { get; set; }
    }

    [DataContract]
    public class ChildRegisteredEvent : AggregateRootEventBase, IChildAdded, IChildLastNameUpdated
    {
        [DataMember]
        public int ChildId { get; set; }

        [DataMember]
        public string LastName { get; set; }

    }
    #endregion

    #region Event stream (service fabric specific)

    [DataContract]
    [KnownType(typeof(PersonRegisteredEvent))]
    [KnownType(typeof(PersonMarriedEvent))]
    public class PersonEventStream : EventStreamBase
    {
    }

    #endregion

    public class Person : AggregateRoot<IPersonEvent>
    {
        public Person()
        {
            RegisterEventAppliers()
                .For<IPersonFirstNameUpdated>(e => FirstName = e.FirstName)
                .For<IMaritalStatusUpdated>(e => MaritalStatus = e.MaritalStatus)
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
            if(string.IsNullOrWhiteSpace(FirstName))
                throw new InvariantsNotMetException(nameof(FirstName));

            base.AssertInvariantsAreMet();
        }
        
        public string FirstName { get; private set; }
        public MaritalStatus MaritalStatus { get; private set; }
        private readonly List<Child> _children = new List<Child>();
        public IReadOnlyCollection<Child> Children => _children.AsReadOnly();
        private int _maxChildId = 0;

        public void Register(Guid aggregateRootId, string firstName, Guid commandId)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new Exception("Invalid first name");

            RaiseEvent(new PersonRegisteredEvent { AggregateRootId = aggregateRootId, FirstName = firstName });
        }

        public void Marry()
        {   
            RaiseEvent(new PersonMarriedEvent { MaritalStatus = MaritalStatus.Married });
        }

        public int RegisterChild(Guid commandId)
        {
            //Business logic
            var lastName = FirstName + "son";
            var childId = _maxChildId + 1;

            RaiseEvent(new ChildRegisteredEvent { ChildId = childId, LastName = lastName });
            return childId;
        }

        public class Child : Entity<Person, IChildEvent>
        {
            public Child(Person aggregateRoot, int id) : base(aggregateRoot)
            {
                ChildId = id;

                RegisterEventAppliers().For<IChildLastNameUpdated>(e => LastName = e.LastName);
            }

            public int ChildId { get; }

            public string LastName { get; private set; }
        }
    }
}
