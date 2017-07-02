using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class EventStoreSession<TEventStream> : IEventStoreSession
            where TEventStream : IEventStream, new()
    {
        private readonly IActorStateManager _stateManager;
        private readonly IDomainEventController _eventController;
        private IEventStored _trackedAggregate;
        public EventStoreSession(IActorStateManager stateManager, IDomainEventController eventController) // TODO: Figure out what to inject here.
        {
            _stateManager = stateManager;
            _eventController = eventController; //todo: something else.
        }

        public async Task<TAggregateRoot> Get<TAggregateRoot>()
            where TAggregateRoot : class, IEventStored, new()
        {
            if (_trackedAggregate == null)
            {
                _trackedAggregate = await ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
                {
                    var eventStream = await _stateManager.GetOrAddStateAsync("state", new TEventStream(), ct);
                    var aggregate = new TAggregateRoot();
                    aggregate.Initialize(_eventController, eventStream.DomainEvents, UtcNowTimeProvider.Instance);
                    return aggregate;
                }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
            }

            return (TAggregateRoot) _trackedAggregate;
        }

        public async Task SaveChanges()
        {
            // TODO: (a) Idempotency check, check for prior execution of this command id. If prior execution has happened, goto c.
            //CommandExecutionHelper
            //await HandleChanges(changes);
            var commandId = ServiceRequestContext.Current?["CommandId"] ?? Guid.NewGuid().ToString();
            
            // TODO: (b) Save commandId, and changes (event ids) in state manager with commandId as key.

            //Save event stream
            await ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
             {
                 var eventStream = await _stateManager.GetOrAddStateAsync("state", new TEventStream(), ct);
                 eventStream.Append(_trackedAggregate.GetChanges().ToArray());
                 await _stateManager.SetStateAsync("state", eventStream, ct);
                 await _stateManager.SaveStateAsync(ct); // TODO: Keep explicit save operation?
                return Task.FromResult(true);
             }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);

            // TODO: (c) Load the commandId from state manager and raise all domain events with ids in that command id record (b).
            //foreach (var domainEvent in _trackedAggregate.GetChanges())
            //{
            //    // TODO: Use logger here.
            //    await _eventController.RaiseDomainEvent(domainEvent);
            //}

            _trackedAggregate.ClearChanges();
        }
        
        public Task Delete()
        {
            // TODO: Handle deletes? 
            throw new NotImplementedException();
        }
    }
}
