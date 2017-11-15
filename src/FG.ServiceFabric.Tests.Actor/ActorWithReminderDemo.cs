using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime.Reminders;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
	[StatePersistence(StatePersistence.Persisted)]
	internal class ActorWithReminderDemo : FG.ServiceFabric.Actors.Runtime.ActorBase, IActorWithReminderDemo, IRemindable
	{
		private IActorReminder _reminder;

		private string _reminderName = @"myReminder";

		public ActorWithReminderDemo(ActorService actorService, ActorId actorId)
			: base(actorService, actorId)
		{
		}

		public async Task<int> GetCountAsync()
		{
			var value = await this.StateManager.TryGetStateAsync<int>("count");

			return value.HasValue ? value.Value : -1;
		}

		public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
		{
			if (reminderName == _reminderName)
			{
				var data = TickUpReminderState.Deserialize(state);

				Console.WriteLine(
					$"Triggered reminder {reminderName} @ {DateTime.UtcNow.ToLocalTime().ToShortTimeString()} with {data.ValueToTickDown} started {(DateTime.UtcNow - data.StartedTime).TotalSeconds} s. ago");

				var count = await this.StateManager.GetStateAsync<int>("count");
				count++;

				await this.StateManager.SetStateAsync("count", count);
			}
		}

		protected override async Task OnActivateAsync()
		{
			ActorDemoEventSource.Current.ActorMessage(this, "Actor activated.");
			if (DateTime.Now.Millisecond % 20 == 0)
			{
				throw new ApplicationException("This millisecond is not good for me, try again soon.");
			}

			await this.StateManager.TryAddStateAsync("count", 0);

			try
			{
				_reminder = this.GetReminder(_reminderName);
			}
			catch (ReminderNotFoundException)
			{
				var valueToTickDown = Environment.TickCount % 5 + 5;
				var data = new TickUpReminderState() {StartedTime = DateTime.UtcNow, ValueToTickDown = valueToTickDown}.Serialize();
				_reminder = await this.RegisterReminderAsync(_reminderName, data, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
			}
		}
	}

	[Serializable]
	[DataContract]
	public class TickUpReminderState : ReminderDataBase<TickUpReminderState>
	{
		[DataMember]
		public long ValueToTickDown { get; set; }

		[DataMember]
		public DateTime StartedTime { get; set; }
	}
}