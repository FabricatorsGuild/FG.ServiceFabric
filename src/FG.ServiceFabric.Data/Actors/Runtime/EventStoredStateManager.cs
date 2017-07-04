using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Persistance;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class EventStoredStateManager<TEventStream> : WrappedStateManager, IEventStoreSession
        where TEventStream : IEventStream, new()
    {
        private readonly IDomainEventController _eventController;
        private readonly IDocumentDbSession _documentDbSession;
        private IEventStored _trackedAggregate;
        private const string EventStreamStateKey = "fg__eventstream";

        public EventStoredStateManager(IActorStateManager innerStateManager, IDomainEventController eventController, IDocumentDbSession documentDbSession) 
            : base(innerStateManager)
        {
            _eventController = eventController;
            _documentDbSession = documentDbSession;
        }

        // Get and start tracking aggregate.
        public Task<TAggregateRoot> GetAsync<TAggregateRoot>() where TAggregateRoot : class, IEventStored, new()
        {
            if (_trackedAggregate != null)
            {
                Task.FromResult((TAggregateRoot) _trackedAggregate);
            }

            return ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var eventStream = await GetOrAddStateAsync(EventStreamStateKey, new TEventStream(), ct);
                _trackedAggregate = new TAggregateRoot();
                _trackedAggregate.Initialize(_eventController, eventStream.DomainEvents, UtcNowTimeProvider.Instance);
                return (TAggregateRoot)_trackedAggregate;
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
        }

        public override async Task SaveStateAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            // Save the changes to the event stream
            await ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var eventStream = await GetOrAddStateAsync(EventStreamStateKey, new TEventStream(), ct);
                eventStream.Append(_trackedAggregate.GetChanges().ToArray());
                await SetStateAsync(EventStreamStateKey, eventStream, ct);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);

            // Save the event ids together with the command id for idempotency checking
            var commandId = ServiceRequestContext.Current?["CommandId"] ?? Guid.NewGuid().ToString();
            var eventIds = _trackedAggregate.GetChanges().Select(e => e.EventId);

            await ExecutionHelper.ExecuteWithRetriesAsync(async ct =>
            {
                var commandExecution = new CommandExecution() {RaisedEventIds = eventIds.ToArray()};
                await AddOrUpdateStateAsync(commandId, commandExecution, (s, execution) => commandExecution, ct);
            }, 3, TimeSpan.FromSeconds(1), CancellationToken.None);
            
            await base.SaveStateAsync(cancellationToken); // This is transactional.

            // TODO: Load the commandId from state manager and raise all domain events from event stream with ids within that command id record.
            //foreach (event)
            //{
            //    // TODO: Use logger here.
            //    // Raise event
            //}
        }
        
        public Task SaveChanges()
        {
            throw new NotImplementedException();
        }

        public Task Delete()
        {
            throw new NotImplementedException();
        }
    }
}