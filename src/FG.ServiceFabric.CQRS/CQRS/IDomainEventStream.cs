using System;

namespace FG.ServiceFabric.CQRS
{
    // TODO: Consider if this should be part of service fabric specific namespace instead.
    public interface IDomainEventStream
    {
        IDomainEvent[] DomainEvents { get; }
        void Append(IDomainEvent domainEvent);
        void Append(IDomainEvent[] domainEvents);
    }
}