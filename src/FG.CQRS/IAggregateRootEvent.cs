using System;

namespace FG.CQRS
{
    public interface IAggregateRootEvent : IDomainEvent
    {
        Guid AggregateRootId { get; set; }
        int Version { get; set; }
    }
}