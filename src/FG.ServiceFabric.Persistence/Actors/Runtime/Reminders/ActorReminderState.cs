using System;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime.Reminders
{
	internal class ActorReminderState : IActorReminderState
	{
		private readonly TimeSpan _nextDueTime;
		private readonly ActorReminderData _reminder;

		public ActorReminderState(ActorReminderData reminder, DateTime currentLogicalTime,
			ActorReminderCompletedData reminderCompletedData)
		{
			this._reminder = reminder;

			if (reminderCompletedData != null)
			{
				this._nextDueTime = ComputeRemainingTime(currentLogicalTime, reminderCompletedData.UtcTime, reminder.Period);
			}
			else
			{
				this._nextDueTime = ComputeRemainingTime(currentLogicalTime, reminder.UtcCreationTime, reminder.DueTime);
			}
		}

		TimeSpan IActorReminderState.RemainingDueTime => this._nextDueTime;

		string IActorReminder.Name => this._reminder.Name;

		TimeSpan IActorReminder.DueTime => this._reminder.DueTime;

		TimeSpan IActorReminder.Period => this._reminder.Period;

		byte[] IActorReminder.State => this._reminder.State;


		private static TimeSpan ComputeRemainingTime(
			DateTime currentLogicalTime,
			DateTime createdOrLastCompletedTime,
			TimeSpan dueTimeOrPeriod)
		{
			var elapsedTime = TimeSpan.Zero;

			if (currentLogicalTime > createdOrLastCompletedTime)
			{
				elapsedTime = currentLogicalTime - createdOrLastCompletedTime;
			}

			// If reminder has negative DueTime or Period, it is not intended to fire again.
			// Skip computing remaining time.
			if (dueTimeOrPeriod < TimeSpan.Zero)
			{
				return dueTimeOrPeriod;
			}

			var remainingTime = TimeSpan.Zero;

			if (dueTimeOrPeriod > elapsedTime)
			{
				remainingTime = dueTimeOrPeriod - elapsedTime;
			}

			return remainingTime;
		}
	}
}