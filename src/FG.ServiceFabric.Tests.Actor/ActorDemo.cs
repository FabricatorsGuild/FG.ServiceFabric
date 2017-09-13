using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime.Reminders;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{

	namespace WithInteralError
	{
		[StatePersistence(StatePersistence.Persisted)]
		internal class ActorDemo : FG.ServiceFabric.Actors.Runtime.ActorBase, IActorDemo
		{
			public ActorDemo(ActorService actorService, ActorId actorId)
				: base(actorService, actorId)
			{
			}

			protected override Task OnActivateAsync()
			{
				ActorDemoEventSource.Current.ActorMessage(this, "Actor activated.");
				if (DateTime.Now.Millisecond % 20 == 0)
				{
					throw new ApplicationException("This millisecond is not good for me, try again soon.");
				}

				return this.StateManager.TryAddStateAsync("count", 1);
			}

			public Task<int> GetCountAsync()
			{
				if (DateTime.Now.Millisecond % 20 == 0)
				{
					throw new ApplicationException("This millisecond is not good for me, try again soon.");
				}
				return this.StateManager.GetStateAsync<int>("count");
			}

			public async Task SetCountAsync(int count)
			{
				if (DateTime.Now.Millisecond % 20 == 0)
				{
					throw new ApplicationException("This millisecond is not good for me, try again soon.");
				}

				ActorDemoEventSource.Current.ActorDemoCountSet(this, count);
				var updatedCount = await this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value);
				ActorDemoEventSource.Current.ActorDemoCountUpdated(this, updatedCount);
			}
		}

	}

	namespace WithoutInternalErrors
	{
		[StatePersistence(StatePersistence.Persisted)]
		public class ActorDemo : FG.ServiceFabric.Actors.Runtime.ActorBase, IActorDemo
		{
			public ActorDemo(ActorService actorService, ActorId actorId)
				: base(actorService, actorId)
			{
			}

			protected override Task OnActivateAsync()
			{
				ActorDemoEventSource.Current.ActorMessage(this, "Actor activated.");
				return this.StateManager.TryAddStateAsync("count", 1);
			}

			public Task<int> GetCountAsync()
			{
				return this.StateManager.GetStateAsync<int>("count");
			}

			public async Task SetCountAsync(int count)
			{
				var updatedCount = await this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value);
			}
		}

		namespace WithMultipleStates
		{
			[DataContract]
			public class ComplexState
			{
				[DataMember]
				public string Value { get; set; }
				[DataMember]
				public DateTime Time { get; set; }
			}

			[StatePersistence(StatePersistence.Persisted)]
			public class ActorDemo : FG.ServiceFabric.Actors.Runtime.ActorBase, IActorDemo
			{
				public ActorDemo(ActorService actorService, ActorId actorId)
					: base(actorService, actorId)
				{
				}

				protected override async Task OnActivateAsync()
				{
					await this.StateManager.TryAddStateAsync("state1", 1);
					await this.StateManager.TryAddStateAsync("state2", $"the value for {this.GetActorId().GetStringId()}");
					await this.StateManager.TryAddStateAsync("state3", new ComplexState(){Time = DateTime.Now, Value = $"Complex state for {this.GetActorId().GetStringId()}" });
					await this.StateManager.TryAddStateAsync("state4", Environment.TickCount % 100);
				}

				public Task<int> GetCountAsync()
				{
					return this.StateManager.GetStateAsync<int>("state1");
				}

				public async Task SetCountAsync(int count)
				{
					var updatedCount = await this.StateManager.AddOrUpdateStateAsync("state1", count, (key, value) => count > value ? count : value);
				}
			}
		}

		namespace WithReminders
		{
			public class MyReminderState : ReminderDataBase<MyReminderState>
			{
				[DataMember]
				public string SomeValue { get; set; }
				[DataMember]
				public string SomeOtherValue { get; set; }
			}

			[StatePersistence(StatePersistence.Persisted)]
			public class ActorDemo : FG.ServiceFabric.Actors.Runtime.ActorBase, IActorDemo, IRemindable
			{
				public ActorDemo(ActorService actorService, ActorId actorId)
					: base(actorService, actorId)
				{

				}

				protected override async Task OnActivateAsync()
				{
					ActorDemoEventSource.Current.ActorMessage(this, "Actor activated.");
					await this.StateManager.TryAddStateAsync("count", 1);
					try
					{
						var reminder = this.GetReminder(@"MyReminder1");
					}
					catch (ReminderNotFoundException)
					{
						var valueToTickDown = Environment.TickCount % 5 + 5;
						var data = new MyReminderState() { SomeValue = DateTime.UtcNow.ToShortTimeString(), SomeOtherValue = valueToTickDown.ToString() }.Serialize(ReminderDataSerializationType.Json);
						var reminder = await this.RegisterReminderAsync(@"MyReminder1", data, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
					}
				}

				public Task<int> GetCountAsync()
				{
					return this.StateManager.GetStateAsync<int>("count");
				}

				public async Task SetCountAsync(int count)
				{
					ActorDemoEventSource.Current.ActorDemoCountSet(this, count);
					var updatedCount = await this.StateManager.AddOrUpdateStateAsync("count", count, (key, value) => count > value ? count : value);
					ActorDemoEventSource.Current.ActorDemoCountUpdated(this, updatedCount);
				}

				public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
				{
					if (reminderName == @"MyReminder1")
					{
						var data = MyReminderState.Deserialize(state, ReminderDataSerializationType.Json);

						Console.WriteLine($"Reminder {nameof(MyReminderState)} {data.SomeValue} {data.SomeOtherValue}");
					}

					return Task.FromResult(true);
				}
			}
		}
	}
}
