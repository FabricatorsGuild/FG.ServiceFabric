using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.CQRS.ReliableMessaging;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.PersonActor.Interfaces
{
    #region Contracts

    public interface IPersonIndexActor : IReliableMessageReceiverActor, IActor
    {
        Task<IEnumerable<Guid>> ListReceivedCommands();
    }
    
    #endregion

    #region Commands
    [DataContract]
    public class IndexCommand : DomainCommandBase
    {
        [DataMember]
        public Guid PersonId { get; set; }
    }
    
    #endregion

    #region Models
   
    #endregion
}