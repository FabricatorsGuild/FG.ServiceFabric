using System;

namespace FG.Common.Utils
{
    public static class Time
    {
        public static bool IsMoreThan(this TimeSpan origin, TimeSpan check)
        {
            return origin > check;
        }

        public static TimeSpan Seconds(this int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        public static TimeSpan Minutes(this int minutes)
        {
            return TimeSpan.FromMinutes(minutes);
        }

        public static TimeSpan Since(DateTime earlierDate)
        {
            return DateTime.Now - earlierDate;
        }
    }
}