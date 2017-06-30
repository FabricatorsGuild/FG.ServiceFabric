using System;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.CQRS.Exceptions;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class EventStoredActor<TAggregateRoot, TEventStream> :
        ActorBase, IDomainEventController
        where TEventStream : IDomainEventStream, new()
        where TAggregateRoot : class, CQRS.IEventStored, new()
    {
        private readonly ITimeProvider _timeProvider;
        public const string CoreStateName = @"state";
        protected TAggregateRoot DomainState = null;
        protected Task<TAggregateRoot> GetAndSetDomainAsync()
        {
            if (DomainState != null) return Task.FromResult(DomainState);

            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var eventStream = await this.StateManager.GetOrAddStateAsync(CoreStateName, new TEventStream(), ct);
                DomainState = new TAggregateRoot();
                DomainState.Initialize(this, eventStream.DomainEvents, _timeProvider);
                return DomainState;
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        protected EventStoredActor(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId, ITimeProvider timeProvider = null) 
            : base(actorService, actorId)
        {
            _timeProvider = timeProvider;
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

        protected Task StoreDomainEventAsync(IDomainEvent domainEvent)
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var eventStream = await this.StateManager.GetOrAddStateAsync<TEventStream>(CoreStateName, new TEventStream(), ct);
                eventStream.Append(domainEvent);
                await this.StateManager.SetStateAsync(CoreStateName, eventStream, ct);
                return Task.FromResult(true);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        public async Task RaiseDomainEvent<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var handleDomainEvent = this as IHandleDomainEvent<TDomainEvent>;
            if (handleDomainEvent == null)
            {
                throw new EventHandlerNotFoundException($"No handler found when raising event of type {typeof(TDomainEvent).FullName}. Did you forget to implement IHandleDomainEvent<{typeof(TDomainEvent).Name}>?");
            }

            await handleDomainEvent.Handle(domainEvent);
        }
        
        public Task<TRequestValue> Request<TRequestValue, TDomainRequest>(TDomainRequest domainRequest) where TDomainRequest : IDomainRequest<TRequestValue>
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var handleDomainRequest = this as IHandleDomainRequest<TRequestValue, TDomainRequest>;
            if (handleDomainRequest == null)
            {
                throw new EventHandlerNotFoundException($"No handler found when handling request of type {typeof(TDomainRequest).FullName}. Did you forget to implement IHandleDomainRequest<{typeof(TDomainRequest).Name}>?");
            }

            var result = handleDomainRequest.Handle(domainRequest);
            return result;
        }

        public Task StoreDomainEventAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent
        {
            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var eventStream = await this.StateManager.GetOrAddStateAsync<TEventStream>(CoreStateName, new TEventStream(), ct);
                eventStream.Append(domainEvent);
                await this.StateManager.SetStateAsync(CoreStateName, eventStream, ct);
                return Task.FromResult(true);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }
    }
}
