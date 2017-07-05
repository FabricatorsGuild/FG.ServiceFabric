using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
    #region Contracts

    public interface IEventStoredActor : IActor
    {
        Task GiveBirthAsync(BornCommand command);
        Task MarryAsync(MarryCommand command);
    }

    public interface IEventStoredActorService : FG.ServiceFabric.Actors.Runtime.IEventStoredActorService
    {
        Task<PersonReadModel> GetAsync(Guid aggregateRootId);
    }

    #endregion

    #region Commands
    [DataContract]
    public class BornCommand : DomainCommandBase
    {
        [DataMember]
        public Guid AggretateRootId { get; set; }
        [DataMember]
        public string Name { get; set; }
    }

    public class MarryCommand : DomainCommandBase
    {
        [DataMember]
        public Guid AggretateRootId { get; set; }
        [DataMember]
        public string Name { get; set; }
    }
    #endregion

    #region Models
    [DataContract]
    public enum MaritalStatus
    {
        [EnumMember]
        Unknown = 0,
        [EnumMember]
        Single = 1,
        [EnumMember]
        Married = 2,
        [EnumMember]
        Divorsed = 3
    }
    
    [DataContract]
    public class PersonReadModel : IAggregateReadModel
    {
        public Guid Id { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public MaritalStatus MaritalStatus { get; set; }
    }
    #endregion
}