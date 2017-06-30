using System;
using System.Linq;
using System.Runtime.Serialization;

//todo: This should realy be part of the ServiceFabric specific implementation.
namespace FG.ServiceFabric.CQRS
{
    [DataContract]
    public abstract class DomainEventStreamBase : IDomainEventStream
    {
        protected DomainEventStreamBase()
        {
            DomainEvents = new IDomainEvent[] { };
        }

        [DataMember]
        public IDomainEvent[] DomainEvents { get; private set; }

        public void Append(IDomainEvent domainEvent)
        {
            DomainEvents = DomainEvents.Union(new IDomainEvent[] { domainEvent }).ToArray();
        }
    }
}