using System.Linq;
using System.Threading;
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

        public Task RegisterAsync(RegisterCommand command)
        {
            return ExecuteCommandAsync(
                ct =>
                {
                    DomainState.Register(command.AggretateRootId, command.Name, command.CommandId);
                },
                command,
                CancellationToken.None);
        }

        public Task MarryAsync(MarryCommand command)
        {
            return ExecuteCommandAsync(
                ct =>
                {
                    DomainState.Marry();
                },
                command,
                CancellationToken.None);
        }

        public Task<int> RegisterChild(RegisterChildCommand command)
        {
            return ExecuteCommandAsync(
                ct =>
                {
                    var childId = DomainState.RegisterChild(command.CommandId);
                    return Task.FromResult(childId);
                },
                command,
                CancellationToken.None);
        }

        public Task Handle(PersonMarriedEvent domainEvent)
        {
            return Task.FromResult("Congratulations");
        }
    }
}
