using System;

namespace FG.ServiceFabric.CQRS
{
    public interface IDomainEvent
    {
        Guid EventId { get; }
        DateTime UtcTimeStamp { get; set; }
    }
}