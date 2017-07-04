using System;
using System.Runtime.Serialization;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.CQRS.Exceptions;
using FG.ServiceFabric.Tests.Actor.Interfaces;

namespace FG.ServiceFabric.Tests.Actor.Domain
{
    #region Domain event interfaces
    public interface IPersonEvent : IAggregateRootEvent { }

    public interface IFirstNameUpdated : IPersonEvent
    {
        string FirstName { get; }
    }
    public interface ILastNameUpdated : IPersonEvent
    {
        string LastName { get; }
    }

    public interface IMaritalStatusUpdated : IPersonEvent
    {
        MaritalStatus MaritalStatus { get; }
    }

    #endregion

    #region Domain events
    [DataContract]
    public class PersonBornEvent : AggregateRootEventBase, IFirstNameUpdated, ILastNameUpdated, IAggregateRootCreatedEvent
    {
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
    }

    [DataContract]
    public class PersonMarriedEvent : AggregateRootEventBase, IMaritalStatusUpdated, ILastNameUpdated
    {
        [DataMember]
        public MaritalStatus MaritalStatus { get; set; }

        [DataMember]
        public string LastName { get; set; }
    }
    #endregion

    #region Event stream (service fabric specific)

    [DataContract]
    [KnownType(typeof(PersonBornEvent))]
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
                .For<IFirstNameUpdated>(e => this.FirstName = e.FirstName)
                .For<ILastNameUpdated>(e => this.LastName = e.LastName)
                .For<IMaritalStatusUpdated>(e => this.MaritalStatus = e.MaritalStatus)
                ;
        }

        public override void AssertInvariantsAreMet()
        {
            if(string.IsNullOrWhiteSpace(FirstName))
                throw new InvariantsNotMetException(nameof(FirstName));

            base.AssertInvariantsAreMet();
        }

        public string LastName { get; private set; }
        public string FirstName { get; private set; }
        public MaritalStatus MaritalStatus { get; private set; }

        public void GiveBirth(Guid aggregateRootId, string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new Exception("Invalid first name");

            //Business logic
            var lastName = firstName + "son";

            RaiseEvent(new PersonBornEvent { AggregateRootId = aggregateRootId, FirstName = firstName, LastName = lastName });
        }

        public void Marry(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                throw new Exception("Invalid last name");

            RaiseEvent(new PersonMarriedEvent { MaritalStatus = MaritalStatus.Married, LastName = lastName});
        }
    }

}
