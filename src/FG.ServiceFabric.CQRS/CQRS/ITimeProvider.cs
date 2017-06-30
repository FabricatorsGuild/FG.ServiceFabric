using System;

namespace FG.ServiceFabric.CQRS
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}