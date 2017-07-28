using System;
using System.Threading;
using System.Threading.Tasks;
using FG.CQRS;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.EventStoredActor
{
    [ActorService(Name = nameof(EventStoredActorService))]
    [StatePersistence(StatePersistence.Persisted)]
    public class EventStoredActor : EventStoredActor<Domain, TheEventStream>, IEventStoredActor,
        IHandleDomainEvent<CreatedEvent>,
        IHandleDomainEvent<InvalidCreatedEvent>,
        IHandleDomainEvent<ChildAddedEvent>
    {
        public EventStoredActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId, outboundMessageChannelLoggerFactory: () => new ActorLogger(true, ""))
        {
        }

        protected override async Task OnActivateAsync()
        {
            await GetAndSetDomainAsync();
            await base.OnActivateAsync();
        }

        public Task CreateAsync(CreateCommand command)
        {
            return ExecuteCommandAsync(
                () => DomainState.Create(this.GetActorId().GetGuidId(), command.SomeProperty),
                command,
                CancellationToken.None);
        }

        public Task CreateInvalidAsync(CreateInvalidCommand command)
        {
            return ExecuteCommandAsync(
                () => DomainState.CreateInvalid(this.GetActorId().GetGuidId(), command.SomeProperty),
                command,
                CancellationToken.None);
        }

        public Task AddChildAsync(AddChildCommand command)
        {
            return ExecuteCommandAsync(
               () => DomainState.AddChild(),
               command,
               CancellationToken.None);
        }
        
        public async Task Handle(CreatedEvent domainEvent)
        {
            await SendMessageAsync<IIndexActor>(ReliableMessage.Create(new IndexCommand { PersonId = domainEvent.AggregateRootId }), new ActorId("Index"), CancellationToken.None, applicationName: "FG.ServiceFabric.Tests.Application");
            await StoreDomainEventAsync(domainEvent);
        }

        public async Task Handle(ChildAddedEvent domainEvent)
        {
            await StoreDomainEventAsync(domainEvent);
        }

        public Task Handle(InvalidCreatedEvent domainEvent)
        {
            throw new Exception("Invalid created!");
        }
    }
}
