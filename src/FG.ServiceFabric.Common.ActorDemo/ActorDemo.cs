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
    internal class ActorDemo : FG.ServiceFabric.Actors.Runtime.ActorBase, IActorDemo
    {
        /// <summary>
        /// Initializes a new instance of ActorDemo
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public ActorDemo(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorDemoEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization

            if (DateTime.Now.Millisecond%20 == 0)
            {
                throw new ApplicationException("This millisecond is not good for me, try again soon.");
            }

            return this.StateManager.TryAddStateAsync("count", 0);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Task<int> GetCountAsync()
        {
            if (DateTime.Now.Millisecond % 20 == 0)
            {
                throw new ApplicationException("This millisecond is not good for me, try again soon.");
            }
            return this.StateManager.GetStateAsync<int>("count");
        }

        /// <summary>
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
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
                        ArrayOfInterfaces = new []{ new SomeImpl() { Value = value }, new SomeImpl { Value = "Foo"}}
                    },
                    new InnerComplexType() { SomeId = Guid.NewGuid()}
                }
            };

            await this.StateManager.SetStateAsync("complexType", complexType);
        }
    }
}
