﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.CQRS;
using FG.ServiceFabric.Actors;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.EventStoredActor
{
    [ActorService(Name = nameof(IndexActor) + "Service")] // Default.
    [StatePersistence(StatePersistence.Persisted)]
    internal class IndexActor : FG.ServiceFabric.Actors.Runtime.ActorBase, IIndexActor, IReliableMessageEndpoint<ICommand>, IHandleCommand<IndexCommand>
    {
        private const string IndexStateKey = "_index_state";

        public IInboundReliableMessageChannel InboundMessageChannel { get; }

        public IndexActor(ActorService actorService, ActorId actorId)
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

        public Task<IEnumerable<Guid>> ListCommandsAsync()
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(
               ct => this.StateManager.GetStateAsync<IEnumerable<Guid>>(IndexStateKey, ct), 3,
               TimeSpan.FromSeconds(1), CancellationToken.None);
        }
        
        public Task Handle(IndexCommand command)
        {
            return UpdateIndexAsync(command);
        }

        private Task UpdateIndexAsync(IndexCommand command)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var index = await this.StateManager.GetOrAddStateAsync(IndexStateKey, new List<Guid>(), ct);
                index.Add(command.CommandId);
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