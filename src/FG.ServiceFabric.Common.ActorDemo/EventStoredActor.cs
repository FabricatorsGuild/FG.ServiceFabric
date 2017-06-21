using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class EventStoredActor : FG.ServiceFabric.Actors.Runtime.ActorBase, IEventStoredActor
    {
        /// <summary>
        /// Initializes a new instance of ActorDemo
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public EventStoredActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task RaiseAsync(string value)
        {
            var @event = new EventImpl
            {
                ListOfStrings = new []{ value },
            };

            await this.StateManager.SetStateAsync(@event.EventId.ToString(), @event);
        }
    }
}
