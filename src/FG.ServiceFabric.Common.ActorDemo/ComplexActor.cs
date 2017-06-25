using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
    [StatePersistence(StatePersistence.Volatile)]
    internal class ComplexActor : FG.ServiceFabric.Actors.Runtime.ActorBase, IComplexActor, IRemindable
    {
        private readonly ComplexActorService _actorService;
        private const string ReminderName = "Remind_me_I_got_a_problem";

        public ComplexActor(ComplexActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            _actorService = actorService;
        }

        protected override async Task OnActivateAsync()
        {
            var reminderRegistration = await this.RegisterReminderAsync(
                ReminderName,
                BitConverter.GetBytes(1337),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));
        }

        public Task<ComplexType> GetComplexTypeAsync()
        {
            return this.StateManager.GetStateAsync<ComplexType>("complexType");
        }

        public async Task SetComplexTypeAsync(string value)
        {
            var complexType = new ComplexType()
            {
                SomeId = Guid.NewGuid(),
                ListOfStrings = new List<string> { "simple" },
                ListOfSomething = new List<InnerComplexType>
                {
                    new InnerComplexType() {
                        SomeId = Guid.NewGuid(),
                        ArrayOfInterfaces = new ISomeInterface[]{ new SomeImpl() { Value = value }, new SomeImpl { Value = "Foo"}}
                    },
                    new InnerComplexType() { SomeId = Guid.NewGuid()}
                }
            };

            await this.StateManager.SetStateAsync("complexType", complexType);
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals(ReminderName))
            {
                var reminder = GetReminder(ReminderName);
                await UnregisterReminderAsync(reminder); //problem resolved this activation...
            }
        }
    }
}
