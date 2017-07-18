using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
    [ActorService(Name = "PersonIndexActorService")] // Default.
    [StatePersistence(StatePersistence.Volatile)]
    internal class PersonIndexActor : FG.ServiceFabric.Actors.Runtime.ActorBase, IPersonIndexActor
    {
        public PersonIndexActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

        public async Task ReceiveMessageAsync(ReliableMessage message)
        {
            await this.StateManager.AddOrUpdateStateAsync("messages", new List<ICommand>(), (key, value) => value.Union(new []{ (ICommand)message.Deserialize()}).ToList());
        }

        public async Task<IEnumerable<Guid>> ListReceivedCommands()
        {
            var commands = await this.StateManager.GetStateAsync<List<ICommand>>("messages");
            return commands.Select(c => c.CommandId);
        }
    }
}
