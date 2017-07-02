using System;
using System.Collections.Generic;
using FG.ServiceFabric.CQRS.Exceptions;

namespace FG.ServiceFabric.CQRS
{

    public abstract partial class AggregateRoot<TAggregateRootEventInterface> : IAggregateRoot, IEventStored
        where TAggregateRootEventInterface : class, IAggregateRootEvent
    {
        private ITimeProvider _timeProvider;
        private IDomainEventController EventController { get; set; }
        private readonly IList<TAggregateRootEventInterface> _uncommittedEvents = new List<TAggregateRootEventInterface>();
        
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
            _eventController.RaiseDomainEvent(domainEvent);
            _uncommittedEvents.Add(domainEvent);
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

        private readonly DomainEventDispatcher<TAggregateRootEventInterface> _eventDispatcher = new DomainEventDispatcher<TAggregateRootEventInterface>();
        
        protected DomainEventDispatcher<TAggregateRootEventInterface>.RegistrationBuilder RegisterEventAppliers()
        {
            return _eventDispatcher.RegisterHandlers();
        }

        #region IEventStored
        private IDomainEventController _eventController;

        public void Initialize(IDomainEventController eventController, IDomainEvent[] eventStream, ITimeProvider timeProvider = null)
        {
            Initialize(eventController, timeProvider);

            if (eventStream == null) return;

            foreach (var domainEvent in eventStream)
            {
                ApplyEvent(domainEvent as TAggregateRootEventInterface);
            }
        }

        public void Initialize(IDomainEventController eventController, ITimeProvider timeProvider = null)
        {
            _timeProvider = timeProvider ?? UtcNowTimeProvider.Instance;
            _eventController = eventController;
        }

        public IEnumerable<IDomainEvent> GetChanges()
        {
            return _uncommittedEvents;
        }

        public void ClearChanges()
        {
            _uncommittedEvents.Clear();
        }

        #endregion
    }
}