using System;

namespace FG.CQRS
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}