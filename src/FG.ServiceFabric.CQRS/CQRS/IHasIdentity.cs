using System;

namespace FG.ServiceFabric.CQRS
{
    public interface IHasIdentity
    {
        Guid Id { get; set; }
    }
}