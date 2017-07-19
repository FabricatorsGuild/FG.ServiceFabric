using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Tests.PersonActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.PersonActor
{
    [ActorService(Name = nameof(PersonIndexActor) + "Service")] // Default.
    [StatePersistence(StatePersistence.Persisted)]
    internal class PersonIndexActor : FG.ServiceFabric.Actors.Runtime.ActorBase, IPersonIndexActor, IReliableMessageEndpoint<ICommand>, IHandleCommand<IndexCommand>
    {
        private const string IndexStateKey = "_index_state";

        public IInboundReliableMessageChannel InboundMessageChannel { get; }

        public PersonIndexActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
            InboundMessageChannel = new InboundReliableMessageChannel<ICommand>(this);
        }

        protected override Task OnActivateAsync()
        {
            return base.OnActivateAsync();
        }

        public async Task ReceiveMessageAsync(ReliableMessage message)
        {
            await InboundMessageChannel.ReceiveMessageAsync(message);
        }

        public Task<IEnumerable<Guid>> ListReceivedCommands()
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
               ct => this.StateManager.GetStateAsync<IEnumerable<Guid>>(IndexStateKey, ct), 3,
               TimeSpan.FromSeconds(1), CancellationToken.None);
        }
        
        public Task Handle(IndexCommand command)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var index = await this.StateManager.GetOrAddStateAsync(IndexStateKey, new List<Guid>(), ct);
                index.Add(command.PersonId);
                await this.StateManager.SetStateAsync(IndexStateKey, index, ct);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        public async Task HandleMessageAsync<TMessage>(TMessage message) where TMessage : ICommand
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var handleDomainEvent = this as IHandleCommand<TMessage>;

            if (handleDomainEvent == null)
                return;

            await handleDomainEvent.Handle(message);
        }

    }
}
