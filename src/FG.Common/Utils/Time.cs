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
			return new TimeSpan(0, 0, 0, seconds);
		}

		public static TimeSpan Minutes(this int minutes)
		{
			return new TimeSpan(0, 0, minutes, 0);
		}

		public static TimeSpan Since(DateTime earlierDate)
		{
			return (DateTime.Now - earlierDate);
		}
	}
}