using System;
using FG.ServiceFabric.CQRS.Exceptions;

namespace FG.ServiceFabric.CQRS
{

    public abstract partial class AggregateRoot<TAggregateRootEventInterface> : IAggregateRoot, IEventStored
        where TAggregateRootEventInterface : class, IAggregateRootEvent
    {
        private ITimeProvider _timeProvider;
        private IDomainEventController EventController { get; set; }
        
        public void Initialize(IDomainEventController domainEventController, IDomainEvent[] eventStream, ITimeProvider timeProvider = null)
        {
            Initialize(domainEventController, timeProvider);

            if(eventStream == null) return;

            foreach(var domainEvent in eventStream)
            {
                ApplyEvent(domainEvent as TAggregateRootEventInterface);
            }
        }

        public void Initialize(IDomainEventController domainEventController, ITimeProvider timeProvider = null)
        {
            _timeProvider = timeProvider ?? UtcNowTimeProvider.Instance;
            EventController = domainEventController;
        }
        
        public Guid AggregateRootId { get; private set; }
        private void SetId(Guid id) { AggregateRootId = id; }

        protected void RaiseEvent<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : TAggregateRootEventInterface
        {
            if(domainEvent is IAggregateRootCreatedEvent)
            {
                if(AggregateRootId != Guid.Empty)
                {
                    throw new AggregateRootException(
                        $"The {nameof(AggregateRootId)} can only be set once. " +
                        $"Only the first event should implement {typeof(IAggregateRootCreatedEvent)}.");
                }

                if(domainEvent.AggregateRootId == Guid.Empty)
                {
                    throw new AggregateRootException($"Missing {nameof(domainEvent.AggregateRootId)}");
                }
            }
            else
            {
                if(AggregateRootId == Guid.Empty)
                {
                    throw new AggregateRootException(
                        $"No {nameof(AggregateRootId)} set. " +
                        $"Did you forget to implement {typeof(IAggregateRootCreatedEvent)} in the first event?");
                }

                if(domainEvent.AggregateRootId != Guid.Empty && domainEvent.AggregateRootId != AggregateRootId)
                {
                    throw new AggregateRootException(
                        $"{nameof(AggregateRootId)} in event is  {domainEvent.AggregateRootId} which is different from the current {AggregateRootId}");
                }

                domainEvent.AggregateRootId = AggregateRootId;
            }

            domainEvent.UtcTimeStamp = _timeProvider.UtcNow;

            ApplyEvent(domainEvent);
            AssertInvariantsAreMet();

            //Store domain event actually calls the state manager which keeps the change pending until the unit of work ends and then it gets persisted by the state provider.
            //todo: Use a pending changes event stream?
            EventController.StoreDomainEventAsync(domainEvent).GetAwaiter().GetResult();
            //todo:remove this and move responsibility to event store.
            EventController.RaiseDomainEvent(domainEvent).GetAwaiter().GetResult();
        }

        public virtual void AssertInvariantsAreMet()
        {
            if (AggregateRootId == Guid.Empty)
            {
                throw new InvariantsNotMetException($"{nameof(AggregateRootId)} not set.");
            }
        }
        
        private void ApplyEvent(TAggregateRootEventInterface domainEvent)
        {
            if (domainEvent is IAggregateRootCreatedEvent)
            {
                SetId(domainEvent.AggregateRootId);
            }

            _eventDispatcher.Dispatch(domainEvent);
        }

        private readonly EventDispatcher<TAggregateRootEventInterface> _eventDispatcher = new EventDispatcher<TAggregateRootEventInterface>();
        protected EventDispatcher<TAggregateRootEventInterface>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventDispatcher.RegisterHandlers();
        }
    }
}