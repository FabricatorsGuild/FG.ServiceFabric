using System;

namespace FG.CQRS
{
    public class UtcNowTimeProvider : ITimeProvider
    {
        public static readonly UtcNowTimeProvider Instance = new UtcNowTimeProvider();
        public DateTime UtcNow => DateTime.UtcNow;
    }
}