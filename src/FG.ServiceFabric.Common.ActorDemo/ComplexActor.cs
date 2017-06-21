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
    [StatePersistence(StatePersistence.Volatile)]
    internal class ComplexActor : FG.ServiceFabric.Actors.Runtime.ActorBase, IComplexActor
    {
        private readonly ComplexActorService _actorService;

        /// <summary>
        /// Initializes a new instance of ActorDemo
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public ComplexActor(ComplexActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            _actorService = actorService;
        }

        protected override async Task OnActivateAsync()
        {
            await _actorService.StateProvider.RestoreExternalState<ComplexType>(this.GetActorId(), "complexType");
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
