using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.Data;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Tests.Actor.Interfaces
{
    public interface IEventStoredActor : IActor
    {
        Task RaiseAsync(string value);
    }

    [DataContract]
    public class EventImpl : IEvent
    {
        public EventImpl()
        {
            EventId = Guid.NewGuid();
        }
        [DataMember]
        public string[] ListOfStrings { get; set; }
        [DataMember]
        public Guid EventId { get; set; }
    }
}