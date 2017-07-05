using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    [DataContract]
    public class CommandExecution
    {
        [DataMember]
        public Guid[] RaisedEventIds { get; set; }
        [DataMember]
        public object ReturnValue { get; set; }
    }


    public class EventStoreSession<TEventStream> : IEventStoreSession
            where TEventStream : IDomainEventStream, new()
    {
        private readonly IActorStateManager _stateManager;
        private readonly IDomainEventController _eventController;
        private IEventStored _trackedAggregate;

        public EventStoreSession(IActorStateManager stateManager, IDomainEventController eventController) 
        {
            _stateManager = stateManager;
            _eventController = eventController; // TODO: Figure out what to inject here.
        }

        public async Task<TAggregateRoot> GetAsync<TAggregateRoot>()
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
            // TODO: Idempotency check, check for prior execution of this command id. If prior execution has happened, goto c.
            
            //Save event stream
            await ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
             {
                 var eventStream = await _stateManager.GetOrAddStateAsync("state", new TEventStream(), ct);
                 eventStream.Append(_trackedAggregate.GetChanges().ToArray());
                 await _stateManager.SetStateAsync("state", eventStream, ct);
                return Task.FromResult(true);
             }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);

            // Save the event ids together with the command id for idempotency checking
            var commandId = ServiceRequestContext.Current?["CommandId"] ?? Guid.NewGuid().ToString();
            var eventIds = _trackedAggregate.GetChanges().Select(e => e.EventId);

            await ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var commandExecution = new CommandExecution() { RaisedEventIds = eventIds.ToArray() };
                await _stateManager.AddOrUpdateStateAsync(commandId, commandExecution, (s, execution) => commandExecution, ct);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);

            // Force save changes.
            await ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                await _stateManager.SaveStateAsync(ct);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);

            _trackedAggregate.ClearChanges();

            // TODO: (c) Load the commandId from state manager and raise all domain events with ids in that command id record (b).
            //foreach (var domainEvent in _trackedAggregate.GetChanges())
            //{
            //    // TODO: Use logger here.
            //    await _eventController.RaiseDomainEvent(domainEvent);
            //}
        }
        
        public Task Delete()
        {
            // TODO: Handle deletes? 
            throw new NotImplementedException();
        }
    }
}
