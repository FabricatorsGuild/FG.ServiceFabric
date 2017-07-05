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
    public class TempEventStoredActor : EventStoredActor<Person, PersonEventStream>, ITempEventStoredActor,
        IHandleDomainEvent<PersonMarriedEvent>
    {
        private Person _aggregate;

        public TempEventStoredActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override async Task OnActivateAsync()
        {
            _aggregate = await EventStoreSession.GetAsync<Person>();
            await base.OnActivateAsync();
        }

        protected override async Task OnPostActorMethodAsync(ActorMethodContext actorMethodContext)
        {
            await EventStoreSession.SaveChanges();
        }

        public Task BornAsync(BornCommand command)
        {
            _aggregate.GiveBirth(command.AggretateRootId, command.Name);
            return Task.FromResult(true);
        }

        public Task MarryAsync(MarryCommand command)
        {
            _aggregate.Marry(command.Name);
            return Task.FromResult(true);
        }

        public Task Handle(PersonMarriedEvent domainEvent)
        {
            return Task.FromResult("Congratulations");
        }
    }
}
