using System;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.CQRS;
using FG.CQRS.Exceptions;
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

        public ITimeProvider TimeProvider { get; }

        protected EventStoredActor(
            Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId,
            ITimeProvider timeProvider = null,
            Func<IOutboundMessageChannelLogger> outboundMessageChannelLoggerFactory = null)
            : base(actorService, actorId)
        {
            TimeProvider = timeProvider;
            OutboundMessageChannel = new OutboundReliableMessageChannel(StateManager, ActorProxyFactory, outboundMessageChannelLoggerFactory);
            InboundMessageChannel = new InboundReliableMessageChannel<ICommand>(this);
        }

        public IOutboundReliableMessageChannel OutboundMessageChannel { get; set; }
        public IInboundReliableMessageChannel InboundMessageChannel { get; set; }

        private IActorTimer _timer;

        protected override Task OnActivateAsync()
        {
            if (_timer == null)
            {
                _timer = RegisterTimer(async _ => { await OutboundMessageChannel.ProcessQueueAsync(); }, null,
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            }

            return base.OnActivateAsync();
        }

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

        public async Task HandleMessageAsync<TMessage>(TMessage message) where TMessage : ICommand
        {
            //    // ReSharper disable once SuspiciousTypeConversion.Global
            var handleDomainEvent = this as IHandleCommand<TMessage>;

            if (handleDomainEvent == null)
                throw new HandlerNotFoundException(
                    $"No handler found for command {nameof(TMessage)}. Did you forget to implement {nameof(IHandleCommand<TMessage>)}?");

            await handleDomainEvent.Handle(message);
        }
    }
}