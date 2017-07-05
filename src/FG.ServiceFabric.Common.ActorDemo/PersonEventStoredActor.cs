using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Tests.Actor.Domain;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.Actor
{
    [StatePersistence(StatePersistence.Volatile)]
    public class PersonEventStoredActor : EventStoredActor<Person, PersonEventStream>, IEventStoredActor, 
        IHandleDomainEvent<PersonMarriedEvent>
    {
        public PersonEventStoredActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override async Task OnActivateAsync()
        {
            await GetAndSetDomainAsync();
            await base.OnActivateAsync();
        }

        public Task GiveBirthAsync(BornCommand command)
        {
            // TODO: Check that command has not already been executed.
            DomainState.GiveBirth(command.AggretateRootId, command.Name);
            return Task.FromResult(true);
        }
        public Task MarryAsync(MarryCommand command)
        {
            // TODO: Check that command has not already been executed.
            DomainState.Marry(command.Name);
            return Task.FromResult(true);
        }
        
        public Task Handle(PersonMarriedEvent domainEvent)
        {
            return Task.FromResult("Congratulations");
        }


    }
}
