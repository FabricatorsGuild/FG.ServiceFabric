using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime.Reminders;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorBase = FG.ServiceFabric.Actors.Runtime.ActorBase;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.Actor
{
    namespace WithInteralError
    {
        [ActorService(Name = "ActorDemoActorService_WithInteralError")]
        [StatePersistence(StatePersistence.Persisted)]
        internal class ActorDemo : ActorBase, IActorDemo
        {
            public ActorDemo(ActorService actorService, ActorId actorId)
                : base(actorService, actorId)
            {
            }

            public Task<int> GetCountAsync()
            {
                if (DateTime.Now.Millisecond % 20 == 0)
                    throw new ApplicationException("This millisecond is not good for me, try again soon.");
                return StateManager.GetStateAsync<int>("count");
            }

            public async Task SetCountAsync(int count)
            {
                if (DateTime.Now.Millisecond % 20 == 0)
                    throw new ApplicationException("This millisecond is not good for me, try again soon.");

                ActorDemoEventSource.Current.ActorDemoCountSet(this, count);
                var updatedCount =
                    await StateManager.AddOrUpdateStateAsync("count", count,
                        (key, value) => count > value ? count : value);
                ActorDemoEventSource.Current.ActorDemoCountUpdated(this, updatedCount);
            }

            protected override Task OnActivateAsync()
            {
                ActorDemoEventSource.Current.ActorMessage(this, "Actor activated.");
                if (DateTime.Now.Millisecond % 20 == 0)
                    throw new ApplicationException("This millisecond is not good for me, try again soon.");

                return StateManager.TryAddStateAsync("count", 1);
            }
        }
    }

    namespace WithoutInternalErrors
    {
        [ActorService(Name = "ActorDemoActorService_WithoutInternalErrors")]
        [StatePersistence(StatePersistence.Persisted)]
        public class ActorDemo : ActorBase, IActorDemo
        {
            public ActorDemo(ActorService actorService, ActorId actorId)
                : base(actorService, actorId)
            {
            }

            public Task<int> GetCountAsync()
            {
                return StateManager.GetStateAsync<int>("count");
            }

            public async Task SetCountAsync(int count)
            {
                var updatedCount =
                    await StateManager.AddOrUpdateStateAsync("count", count,
                        (key, value) => count > value ? count : value);
            }

            protected override Task OnActivateAsync()
            {
                ActorDemoEventSource.Current.ActorMessage(this, "Actor activated.");
                return StateManager.TryAddStateAsync("count", 1);
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

            [ActorService(Name =
                "ActorDemoActorService_WithoutInternalErrors_WithMultipleStates")]
            [StatePersistence(StatePersistence.Persisted)]
            public class ActorDemo : ActorBase, IActorDemo
            {
                public ActorDemo(ActorService actorService, ActorId actorId)
                    : base(actorService, actorId)
                {
                }

                public Task<int> GetCountAsync()
                {
                    return StateManager.GetStateAsync<int>("state1");
                }

                public async Task SetCountAsync(int count)
                {
                    var updatedCount =
                        await StateManager.AddOrUpdateStateAsync("state1", count,
                            (key, value) => count > value ? count : value);
                }

                protected override async Task OnActivateAsync()
                {
                    await StateManager.TryAddStateAsync("state1", 1);
                    await StateManager.TryAddStateAsync("state2", $"the value for {this.GetActorId().GetStringId()}");
                    await StateManager.TryAddStateAsync("state3",
                        new ComplexState
                        {
                            Time = DateTime.Now,
                            Value = $"Complex state for {this.GetActorId().GetStringId()}"
                        });
                    await StateManager.TryAddStateAsync("state4", Environment.TickCount % 100);
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

            [ActorService(Name =
                "ActorDemoActorService_WithoutInternalErrors_WithReminders")]
            [StatePersistence(StatePersistence.Persisted)]
            public class ActorDemo : ActorBase, IActorDemo, IRemindable
            {
                public ActorDemo(ActorService actorService, ActorId actorId)
                    : base(actorService, actorId)
                {
                }

                public Task<int> GetCountAsync()
                {
                    return StateManager.GetStateAsync<int>("count");
                }

                public async Task SetCountAsync(int count)
                {
                    ActorDemoEventSource.Current.ActorDemoCountSet(this, count);
                    var updatedCount =
                        await StateManager.AddOrUpdateStateAsync("count", count,
                            (key, value) => count > value ? count : value);
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

                protected override async Task OnActivateAsync()
                {
                    ActorDemoEventSource.Current.ActorMessage(this, "Actor activated.");
                    await StateManager.TryAddStateAsync("count", 1);
                    try
                    {
                        var reminder = GetReminder(@"MyReminder1");
                    }
                    catch (ReminderNotFoundException)
                    {
                        var valueToTickDown = Environment.TickCount % 5 + 5;
                        var data = new MyReminderState
                        {
                            SomeValue = DateTime.UtcNow.ToShortTimeString(),
                            SomeOtherValue = valueToTickDown.ToString()
                        }.Serialize(ReminderDataSerializationType.Json);
                        var reminder =
                            await RegisterReminderAsync(@"MyReminder1", data, TimeSpan.FromSeconds(2),
                                TimeSpan.FromSeconds(2));
                    }
                }
            }
        }
    }
}