using System;
using System.Linq;
using System.Runtime.Serialization;
using FG.CQRS;

namespace FG.ServiceFabric.Actors.Runtime
{
    [DataContract]
    public abstract class EventStreamStateBase : IDomainEventStream
    {
        protected EventStreamStateBase()
        {
            DomainEvents = new IDomainEvent[] { };
        }

        [DataMember]
        public IDomainEvent[] DomainEvents { get; private set; }

        public void Append(IDomainEvent domainEvent)
        {
            DomainEvents = DomainEvents.Union(new[] { domainEvent }).ToArray();

            // Raise the event
            EventAppended?.Invoke(this, domainEvent);
        }

        public event EventHandler<IDomainEvent> EventAppended;
    }
}