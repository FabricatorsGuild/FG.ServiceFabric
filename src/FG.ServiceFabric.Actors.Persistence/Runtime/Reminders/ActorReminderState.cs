using System;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime.Reminders
{
    internal class ActorReminderState : IActorReminderState
    {
        private readonly TimeSpan _nextDueTime;
        private readonly ActorReminderData _reminder;

        public ActorReminderState(ActorReminderData reminder, DateTime currentLogicalTime)
        {
            _reminder = reminder;
            if (reminder.IsComplete)
                _nextDueTime = ComputeRemainingTime(currentLogicalTime, reminder.UtcCompletedTime, reminder.DueTime);
            else
                _nextDueTime = ComputeRemainingTime(currentLogicalTime, reminder.UtcCreationTime, reminder.DueTime);
        }

        public ActorReminderState(ActorReminderData reminder, DateTime currentLogicalTime,
            ActorReminderCompletedData reminderCompletedData)
        {
            _reminder = reminder;

            if (reminderCompletedData != null)
                _nextDueTime = ComputeRemainingTime(currentLogicalTime, reminderCompletedData.UtcTime, reminder.Period);
            else
                _nextDueTime = ComputeRemainingTime(currentLogicalTime, reminder.UtcCreationTime, reminder.DueTime);
        }

        TimeSpan IActorReminderState.RemainingDueTime => _nextDueTime;

        string IActorReminder.Name => _reminder.Name;

        TimeSpan IActorReminder.DueTime => _reminder.DueTime;

        TimeSpan IActorReminder.Period => _reminder.Period;

        byte[] IActorReminder.State => _reminder.State;


        private static TimeSpan ComputeRemainingTime(
            DateTime currentLogicalTime,
            DateTime createdOrLastCompletedTime,
            TimeSpan dueTimeOrPeriod)
        {
            var elapsedTime = TimeSpan.Zero;

            if (currentLogicalTime > createdOrLastCompletedTime)
                elapsedTime = currentLogicalTime - createdOrLastCompletedTime;

            // If reminder has negative DueTime or Period, it is not intended to fire again.
            // Skip computing remaining time.
            if (dueTimeOrPeriod < TimeSpan.Zero)
                return dueTimeOrPeriod;

            var remainingTime = TimeSpan.Zero;

            if (dueTimeOrPeriod > elapsedTime)
                remainingTime = dueTimeOrPeriod - elapsedTime;

            return remainingTime;
        }
    }
}