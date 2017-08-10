using System;
using System.Runtime.Serialization;

namespace FG.CQRS
{
    [DataContract]
    public abstract class AggregateRootEventBase : IAggregateRootEvent
    {
        protected AggregateRootEventBase()
        {
            EventId = Guid.NewGuid();
            UtcTimeStamp = DateTime.UtcNow;
        }

        [DataMember]
        public Guid EventId { get; private set; }
        [DataMember]
        public DateTime UtcTimeStamp { get; set; }
        [DataMember]
        public Guid AggregateRootId { get; set; }
        [DataMember]
        public int Version { get; set; }
    }
}