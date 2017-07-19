using System;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Tests.PersonActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.PersonActor
{
    [ActorService(Name = nameof(PersonActorService))]
    [StatePersistence(StatePersistence.Persisted)]
    public class PersonActor : EventStoredActor<Person, PersonEventStream>, IPersonActor,
        IHandleDomainEvent<PersonMarriedEvent>,
        IHandleDomainEvent<PersonRegisteredEvent>,
        IHandleDomainEvent<ChildRegisteredEvent>
    {
        public PersonActor(ActorService actorService, ActorId actorId)
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
                () => DomainState.Register(command.AggretateRootId, command.Name),
                command,
                CancellationToken.None);
        }

        public Task MarryAsync(MarryCommand command)
        {
            return ExecuteCommandAsync(
                () => DomainState.Marry(),
                command,
                CancellationToken.None);
        }

        public Task<int> RegisterChild(RegisterChildCommand command)
        {
            return ExecuteCommandAsync(
                ct => Task.FromResult(DomainState.RegisterChild(command.CommandId)),
                command,
                CancellationToken.None);
        }

        public async Task Handle(PersonMarriedEvent domainEvent)
        {
            await StoreDomainEventAsync(domainEvent);
        }

        public async Task Handle(PersonRegisteredEvent domainEvent)
        {
            await OutboundMessageChannel.SendMessageAsync<IPersonIndexActor>(ReliableMessage.Create(new IndexCommand { PersonId = domainEvent.AggregateRootId }), new ActorId("PersonIndex"));
            await StoreDomainEventAsync(domainEvent);
        }

        public async Task Handle(ChildRegisteredEvent domainEvent)
        {
            await StoreDomainEventAsync(domainEvent);
        }
    }
}
