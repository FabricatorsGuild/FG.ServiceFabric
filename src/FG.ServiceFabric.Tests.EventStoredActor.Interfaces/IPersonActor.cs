using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.CQRS;
using FG.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.EventStoredActor.Interfaces
{
    #region Contracts

    public interface IEventStoredActor : IActor
    {
        Task CreateAsync(CreateCommand command);
        Task CreateInvalidAsync(CreateInvalidCommand command);
        Task AddChildAsync(AddChildCommand command);
    }

    public interface IEventStoredActorService : FG.ServiceFabric.Actors.Runtime.IEventStoredActorService
    {
        Task<ReadModel> GetAsync(Guid aggregateRootId);
    }

    #endregion

    #region Commands

    [DataContract]
    public class CreateCommand : DomainCommandBase
    {
        [DataMember]
        public string SomeProperty { get; set; }
    }

    [DataContract]
    public class CreateInvalidCommand : DomainCommandBase
    {
        [DataMember]
        public string SomeProperty { get; set; }
    }

    public class AddChildCommand : DomainCommandBase
    {
        [DataMember]
        public Guid AggretateRootId { get; set; }

        [DataMember]
        public string ChildProperty { get; set; }
    }

    #endregion

    #region Models

    [DataContract]
    public class ReadModel : IAggregateReadModel
    {
        [DataMember]
        public string SomeProperty { get; set; }

        public Guid Id { get; set; }
    }

    #endregion
}