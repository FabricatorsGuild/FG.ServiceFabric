using System;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.Common.Utils;
using FG.CQRS;
using FG.CQRS.Exceptions;
using FG.ServiceFabric.Diagnostics;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class EventStoredActor<TAggregateRoot, TEventStream> : ActorBase, 
        IDomainEventController, 
        IReliableMessageEndpoint<ICommand>
        where TEventStream : IDomainEventStream, new()
        where TAggregateRoot : class, IEventStored, new()
    {
        public const string EventStreamStateKey = @"fg__eventstream_state";

        protected ITimeProvider TimeProvider { get; }
        protected TimeSpan OutboundMessageChannelPeriod { get; }
        protected IOutboundReliableMessageChannel OutboundMessageChannel { get; set; }
        protected IInboundReliableMessageChannel InboundMessageChannel { get; set; }

        protected EventStoredActor(
            Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId,
            ITimeProvider timeProvider = null,
            Func<IOutboundMessageChannelLogger> outboundMessageChannelLoggerFactory = null,
            TimeSpan? outboundMessageChannelPeriod = null)
            : base(actorService, actorId)
        {
            TimeProvider = timeProvider;
            OutboundMessageChannel = new OutboundReliableMessageChannel(
                stateManager: StateManager, 
                actorProxyFactory: ActorProxyFactory, 
                loggerFactory: outboundMessageChannelLoggerFactory,
                messageDrop: OnMessageDropAsync
                );
            InboundMessageChannel = new InboundReliableMessageChannel<ICommand>(this);
            OutboundMessageChannelPeriod = outboundMessageChannelPeriod ?? 5.Seconds();
        }


        private IActorTimer _outboundMessageChannelTimer;
        protected override Task OnActivateAsync()
        {
            if (_outboundMessageChannelTimer == null)
            {
                _outboundMessageChannelTimer = RegisterTimer(async _ => { await OutboundMessageChannel.ProcessQueueAsync(); }, null,
                    1.Seconds(), OutboundMessageChannelPeriod);
            }

            return base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            if (_outboundMessageChannelTimer != null)
            {
                UnregisterTimer(_outboundMessageChannelTimer);
            }

            return base.OnDeactivateAsync();
        }

        #region Command deduplication

        protected async Task ExecuteCommandAsync
            (Func<CancellationToken, Task> func, ICommand command, CancellationToken cancellationToken)
        {
            await CommandDeduplicationHelper.ProcessOnceAsync(func, command, StateManager, cancellationToken);
        }

        protected async Task ExecuteCommandAsync
            (Action action, ICommand command, CancellationToken cancellationToken)
        {
            await CommandDeduplicationHelper.ProcessOnceAsync(action, command, StateManager, CancellationToken.None);
        }

        protected async Task<T> ExecuteCommandAsync<T>
            (Func<CancellationToken, Task<T>> func, ICommand command, CancellationToken cancellationToken)
            where T : struct
        {
            return await CommandDeduplicationHelper.ProcessOnceAsync(func, command, StateManager, cancellationToken);
        }

        #endregion

        #region Reliable messaging

        protected Task SendMessageAsync<TActorInterface>(ReliableMessage message, ActorId actorId, string applicationName = null,
            string serviceName = null, string listerName = null)
            where TActorInterface : IReliableMessageReceiverActor
        {
            return OutboundMessageChannel.SendMessageAsync<TActorInterface>(message, actorId, applicationName, serviceName,
                listerName);
        }

        public async Task HandleMessageAsync<TMessage>(TMessage message) where TMessage : ICommand
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var handleDomainEvent = this as IHandleCommand<TMessage>;

            if (handleDomainEvent == null)
                throw new HandlerNotFoundException(
                    $"No handler found for command {nameof(TMessage)}. Did you forget to implement {nameof(IHandleCommand<TMessage>)}?");

            await handleDomainEvent.Handle(message);
        }

        protected virtual Task OnMessageDropAsync(ActorReliableMessage actorReliableMessage)
        {
            return null; // Will result in message ending up in dead letter queue.
        }

        #endregion

        protected TAggregateRoot DomainState = null;

        protected Task<TAggregateRoot> GetAndSetDomainAsync()
        {
            if (DomainState != null) return Task.FromResult(DomainState);

            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var eventStream = await this.StateManager.GetOrAddStateAsync(EventStreamStateKey, new TEventStream(), ct);
                DomainState = new TAggregateRoot();
                DomainState.Initialize(this, eventStream.DomainEvents, TimeProvider);
                return DomainState;
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        protected Task StoreDomainEventAsync(IDomainEvent domainEvent)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var eventStream = await this.StateManager.GetOrAddStateAsync(EventStreamStateKey, new TEventStream(), ct);
                eventStream.Append(domainEvent);
                await this.StateManager.SetStateAsync(EventStreamStateKey, eventStream, ct);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        public async Task RaiseDomainEventAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var handleDomainEvent = this as IHandleDomainEvent<TDomainEvent>;

            if (handleDomainEvent == null)
                throw new HandlerNotFoundException(
                    $"No handler found for event {nameof(TDomainEvent)}. Did you forget to implement {nameof(IHandleDomainEvent<TDomainEvent>)}?");

            await handleDomainEvent.Handle(domainEvent);
        }
    }
}