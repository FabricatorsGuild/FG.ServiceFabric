using System.Collections.Immutable;
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
            InnerEvents = ImmutableList<IDomainEvent>.Empty;
        }

        [DataMember]
        private ImmutableList<IDomainEvent> InnerEvents { get; set; }

        public void Append(IDomainEvent domainEvent)
        {
            InnerEvents = InnerEvents.Add(domainEvent);
        }

        public IDomainEvent[] DomainEvents => InnerEvents.ToArray();
    }
}