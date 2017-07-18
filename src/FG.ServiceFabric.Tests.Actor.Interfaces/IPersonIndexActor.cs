using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
    #region Contracts

    public interface IPersonIndexActor : IReliableMessageReceiver, IActor
    {
        Task<IEnumerable<Guid>> ListReceivedCommands();
    }
    
    #endregion

    #region Commands
    [DataContract]
    public class IndexCommand : DomainCommandBase
    {
        public Guid PersonId { get; set; }
    }
    
    #endregion

    #region Models
   
    #endregion
}