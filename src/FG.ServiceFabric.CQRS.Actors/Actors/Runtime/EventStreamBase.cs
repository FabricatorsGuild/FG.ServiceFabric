using System.Linq;
using System.Runtime.Serialization;
using FG.ServiceFabric.CQRS;

namespace FG.ServiceFabric.Actors.Runtime
{
    [DataContract]
    public abstract class EventStreamBase : IDomainEventStream
    {
        protected EventStreamBase()
        {
            DomainEvents = new IDomainEvent[] { };
        }

        [DataMember]
        public IDomainEvent[] DomainEvents { get; private set; }

        public void Append(IDomainEvent domainEvent)
        {
            DomainEvents = DomainEvents.Union(new[] { domainEvent }).ToArray();
        }

        public void Append(IDomainEvent[] domainEvents)
        {
            DomainEvents = DomainEvents.Union(domainEvents).ToArray();
        }
    }
}