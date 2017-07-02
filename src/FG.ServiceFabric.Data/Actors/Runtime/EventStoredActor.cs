using System;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class EventStoredActor : ActorBase, IDomainEventController
       
    {
        private readonly ITimeProvider _timeProvider;
        public const string EventStreamStateKey = @"fg__eventstream_state";
       
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
