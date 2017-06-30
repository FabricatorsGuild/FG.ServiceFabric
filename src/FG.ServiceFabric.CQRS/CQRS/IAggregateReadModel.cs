using System;

namespace FG.ServiceFabric.CQRS
{
    public interface IAggregateReadModel
    {
        Guid Id { get; set; }    
    }
}
