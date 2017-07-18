using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.CQRS.ReliableMessaging
{
    [DataContract]
    public class ActorReliableMessage
    {
        [DataMember]
        public ActorReference ActorReference { get; set; }
        [DataMember]
        public ReliableMessage Message { get; set; }
    }
}