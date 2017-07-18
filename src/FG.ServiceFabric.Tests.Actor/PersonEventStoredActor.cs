using System;
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
    [ActorService(Name = nameof(PersonEventStoredActor))]
    [StatePersistence(StatePersistence.Volatile)]
    public class PersonEventStoredActor : EventStoredActor<Person, PersonEventStream>, IPersonEventStoredActor,
        IHandleDomainEvent<PersonMarriedEvent>,
        IHandleDomainEvent<PersonRegisteredEvent>,
        IHandleDomainEvent<ChildRegisteredEvent>
    {
        public PersonEventStoredActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected override async Task OnActivateAsync()
        {
            await GetAndSetDomainAsync();
            await InitializeReliableMessageQueue();
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

        public async Task Handle(PersonMarriedEvent domainEvent)
        {
            await StoreDomainEventAsync(domainEvent);
        }
        
        public async Task Handle(PersonRegisteredEvent domainEvent)
        {
            var actorProxy = ActorProxyFactory.CreateActorProxy<IPersonIndexActor>(new ActorId("PersonIndex"));
            var actorReference = ActorReference.Get(actorProxy);

            // Since this command will be asynchronous and pass the state of this actor (transaction), we can assure it sent at least once..
            await ReliablySendMessageAsync(
                new ReliableActorMessage(new IndexCommand { PersonId = domainEvent.AggregateRootId}, actorReference));

            await StoreDomainEventAsync(domainEvent);
        }

        public async Task Handle(ChildRegisteredEvent domainEvent)
        {
            await StoreDomainEventAsync(domainEvent);
        }
    }
}
