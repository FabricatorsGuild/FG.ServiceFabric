using System;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.CQRS;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class EventStoredActor<TAggregateRoot, TEventStream> : ActorBase, IDomainEventController 
        where TEventStream : IDomainEventStream, new()
        where TAggregateRoot : class, IEventStored, new()
    {
        public const string EventStreamStateKey = @"fg__eventstream_state";
       
        protected EventStoredActor(
            Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId, 
            ITimeProvider timeProvider = null, 
            Func<IActorStateManager, IEventStoreSession> eventStoreSessionFactory = null) 
            : base(actorService, actorId)
        {
            TimeProvider = timeProvider;
            eventStoreSessionFactory = eventStoreSessionFactory ?? (sm => new EventStoreSession<TEventStream>(sm, this));
            EventStoreSession = eventStoreSessionFactory(StateManager);
        }
        
        public IEventStoreSession EventStoreSession { get; }
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
                return;
            
            await handleDomainEvent.Handle(domainEvent);
        }
    }
}
