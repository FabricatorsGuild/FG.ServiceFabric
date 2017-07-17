using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.CQRS.Exceptions;
using FG.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class EventStoredActor<TAggregateRoot, TEventStream> : ActorBase, IDomainEventController, IReliableMessageReceiver
        where TEventStream : IDomainEventStream, new()
        where TAggregateRoot : class, IEventStored, new()
    {
        public const string EventStreamStateKey = @"fg__eventstream_state";
        public const string ReliableMessageQueueStateKey = @"fg__reliablemessagequeue_state";
        public const string DeadLetterStateKey = @"fg__deadletter_state";
       
        protected EventStoredActor(
            Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId, 
            ITimeProvider timeProvider = null) 
            : base(actorService, actorId)
        {
            TimeProvider = timeProvider;
        }
        
        public ITimeProvider TimeProvider { get; }
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

        private IActorTimer _timer;

        protected Task InitializeReliableMessageQueue()
        {
            if (_timer == null)
            {
                _timer = RegisterTimer(async _ =>
                {
                    await ProcessQueueAsync();
                }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            }

            return
                ExecutionHelper.ExecuteWithRetriesAsync(
                    ct =>
                        this.StateManager.GetOrAddStateAsync(ReliableMessageQueueStateKey, new ReliableMessageQueue(),
                            ct), 3, TimeSpan.FromSeconds(1), CancellationToken.None);

        }

        private async Task ProcessQueueAsync()
        {
            var queue =
                await ExecutionHelper.ExecuteWithRetriesAsync(
                    ct => this.StateManager.GetStateAsync<Queue<ReliableActorMessage>>(ReliableMessageQueueStateKey, ct),
                    3, TimeSpan.FromSeconds(1), CancellationToken.None);

            await SendMessagesAsync(queue);
        }

        private async Task SendMessagesAsync(Queue<ReliableActorMessage> queue)
        {
            while (queue.Count > 0)
            {
                // TODO: Error handling and logging.
                var message = queue.Dequeue();
                await SendMessageAsync(message);
                // TODO: On error, move to dead letter (retry?)
            }
        }

        protected async Task ExecuteCommandAsync
           (Func<CancellationToken, Task> func, ICommand command, CancellationToken cancellationToken)
        {
            await CommandExecutionHelper.ExecuteCommandAsync(func, command, StateManager, cancellationToken);
        }

        protected async Task ExecuteCommandAsync
            (Action<CancellationToken> action, ICommand command, CancellationToken cancellationToken)
        {
            await CommandExecutionHelper.ExecuteCommandAsync(action, command, StateManager, cancellationToken);
        }

        protected async Task<T> ExecuteCommandAsync<T>
            (Func<CancellationToken, Task<T>> func, ICommand command, CancellationToken cancellationToken)
            where T : struct
        {
            return await CommandExecutionHelper.ExecuteCommandAsync(func, command, StateManager, cancellationToken);
        }

        public Task StoreDomainEventAsync(IDomainEvent domainEvent)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var eventStream = await this.StateManager.GetOrAddStateAsync<TEventStream>(EventStreamStateKey, new TEventStream(), ct);
                eventStream.Append(domainEvent);
                await this.StateManager.SetStateAsync(EventStreamStateKey, eventStream, ct);
                return Task.FromResult(true);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        public async Task RaiseDomainEvent<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var handleDomainEvent = this as IHandleDomainEvent<TDomainEvent>;

            if (handleDomainEvent == null)
                throw new EventHandlerNotFoundException($"No handler found for event {nameof(TDomainEvent)}. Did you forget to implement {nameof(IHandleDomainEvent<TDomainEvent>)}?");
            
            await handleDomainEvent.Handle(domainEvent);
        }

        public abstract Task ReceiveMessageAsync(ReliableMessage message);

        public Task ReliablySendMessageAsync(ReliableActorMessage message)
        {
            // TODO: Proper state handling.
            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var queue = await this.StateManager.GetOrAddStateAsync(ReliableMessageQueueStateKey, new ReliableMessageQueue(), ct);
                queue.Queue.Enqueue(message);
                await this.StateManager.SetStateAsync(EventStreamStateKey, queue, ct);
                return Task.FromResult(true);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        private Task SendMessageAsync(ReliableActorMessage message)
        {
            return ((IReliableMessageReceiver)message.ActorReference
               .Bind(typeof(IReliableMessageReceiver)))
               .ReceiveMessageAsync(message);
        }
    }
}
