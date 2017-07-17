using System;

namespace FG.ServiceFabric.CQRS
{
    public interface IDomainEventStream
    {
        IDomainEvent[] DomainEvents { get; }
        void Append(IDomainEvent domainEvent);
        void Append(IDomainEvent[] domainEvents);
    }
}