using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.CQRS.Exceptions;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class ReadModelGenerator<TAggregateRootEventInterface, TReadModel> : IDisposable
        where TAggregateRootEventInterface : class, IAggregateRootEvent
        where TReadModel : class, new()
    {
        protected readonly DomainEventDispatcher<TAggregateRootEventInterface> EventDispatcher = new DomainEventDispatcher<TAggregateRootEventInterface>();
        
        protected DomainEventDispatcher<TAggregateRootEventInterface>.RegistrationBuilder RegisterEventAppliers()
        {
            return EventDispatcher.RegisterHandlers();
        }

        protected TReadModel ReadModel { get; set; }

        public void Apply(TReadModel readModel, TAggregateRootEventInterface evt)
        {
            ReadModel = readModel;
            EventDispatcher.Dispatch(evt);
            ReadModel = null;
        }

        public void Dispose()
        { }
    }

    public abstract class NestedEntityReadModelGenerator<TAggregateRootEventInterface, TReadModel> : ReadModelGenerator<TAggregateRootEventInterface, TReadModel>
    where TAggregateRootEventInterface : class, IAggregateRootEvent
    where TReadModel : class, new()
    { }

    public abstract class AggregateRootReadModelGenerator<TEventStream, TAggregateRootEventInterface, TReadModel> 
        : ReadModelGenerator<TAggregateRootEventInterface, TReadModel>
        where TEventStream : class, IDomainEventStream, new()
        where TAggregateRootEventInterface : class, IAggregateRootEvent
        where TReadModel : class, IAggregateReadModel, new()
    {
        private readonly IEventStreamReader<TEventStream> _eventStreamReader;

        protected AggregateRootReadModelGenerator(IEventStreamReader<TEventStream> eventStreamReader)
        {
            _eventStreamReader = eventStreamReader;

            RegisterEventAppliers()
                .For<TAggregateRootEventInterface>(e => ReadModel.Id = e.AggregateRootId);
        }

        public async Task<TReadModel> GenerateAsync(Guid aggregateRootId, CancellationToken cancellationToken)
        {
            return await GenerateFromEventStreamAsync(aggregateRootId, cancellationToken);
        }

        public async Task<TReadModel> GenerateAsync(Guid aggregateRootId, CancellationToken cancellationToken, DateTime pointInDateTime)
        {
            return await GenerateFromEventStreamAsync(aggregateRootId, cancellationToken, pointInDateTime);
        }
        
        private async Task<TReadModel> GenerateFromEventStreamAsync(Guid aggregateRootId, CancellationToken cancellationToken, DateTime pointInTime = default(DateTime))
        {
            var eventStream = await _eventStreamReader.GetEventStreamAsync(aggregateRootId, cancellationToken);
            var domainEvents = pointInTime == default(DateTime)
                                   ? eventStream.DomainEvents
                                   : eventStream.DomainEvents.TakeWhile(e => e.UtcTimeStamp <= pointInTime).ToArray();

            if (domainEvents.Length == 0)
            {
                throw new AggregateRootException($"Aggregate root {aggregateRootId} exists but event stream does not include any domain events.");
            }
            else if (!(domainEvents[0] is IAggregateRootCreatedEvent))
            {
                throw new AggregateRootException($"Expected first event of aggregate root {aggregateRootId} to be of type {nameof(IAggregateRootCreatedEvent)}");
            }

            ReadModel = new TReadModel();
            foreach (var domainEvent in domainEvents)
            {
                EventDispatcher.Dispatch(domainEvent as TAggregateRootEventInterface);
            }
            var result = ReadModel;
            ReadModel = null;
            return result;
        }
    }
}