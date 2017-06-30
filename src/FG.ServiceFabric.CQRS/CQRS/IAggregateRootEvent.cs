using System;

namespace FG.ServiceFabric.CQRS
{
    public interface IAggregateRootEvent : IDomainEvent
    {
        Guid AggregateRootId { get; set; }
    }
}