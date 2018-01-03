using System;
using System.Linq;
using System.Runtime.Serialization;

namespace FG.CQRS
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
            DomainEvents = DomainEvents.Union(new[] {domainEvent}).ToArray();

            // Raise the event
            EventAppended?.Invoke(this, domainEvent);
        }

        public event EventHandler<IDomainEvent> EventAppended;
    }
}