using System;
using FG.CQRS.Exceptions;

namespace FG.CQRS
{
	public abstract partial class AggregateRoot<TAggregateRootEventInterface> : IAggregateRoot, IEventStored
		where TAggregateRootEventInterface : class, IAggregateRootEvent
	{
		private readonly EventDispatcher<TAggregateRootEventInterface> _eventDispatcher =
			new EventDispatcher<TAggregateRootEventInterface>();

		private ITimeProvider _timeProvider;
		protected int Version;

		public Guid AggregateRootId { get; private set; }

		private void SetId(Guid id)
		{
			AggregateRootId = id;
		}

		protected void RaiseEvent<TDomainEvent>(TDomainEvent aggregateRootEvent)
			where TDomainEvent : TAggregateRootEventInterface
		{
			if (aggregateRootEvent is IAggregateRootCreatedEvent)
			{
				if (Version != 0)
				{
					throw new AggregateRootException(
						$"Expected the event implementing {typeof(IAggregateRootCreatedEvent)} to be first version.");
				}

				if (AggregateRootId != Guid.Empty)
				{
					throw new AggregateRootException(
						$"The {nameof(AggregateRootId)} can only be set once. " +
						$"Only the first event should implement {typeof(IAggregateRootCreatedEvent)}.");
				}

				if (aggregateRootEvent.AggregateRootId == Guid.Empty)
				{
					throw new AggregateRootException($"Missing {nameof(aggregateRootEvent.AggregateRootId)}");
				}
			}
			else
			{
				if (AggregateRootId == Guid.Empty)
				{
					throw new AggregateRootException(
						$"No {nameof(AggregateRootId)} set. " +
						$"Did you forget to implement {typeof(IAggregateRootCreatedEvent)} in the first event?");
				}

				if (aggregateRootEvent.AggregateRootId != Guid.Empty && aggregateRootEvent.AggregateRootId != AggregateRootId)
				{
					throw new AggregateRootException(
						$"{nameof(AggregateRootId)} in event is  {aggregateRootEvent.AggregateRootId} which is different from the current {AggregateRootId}");
				}

				aggregateRootEvent.AggregateRootId = AggregateRootId;
			}

			aggregateRootEvent.Version = Version + 1;
			aggregateRootEvent.UtcTimeStamp = _timeProvider.UtcNow;

			ApplyEvent(aggregateRootEvent);
			AssertInvariantsAreMet();
			_eventController.RaiseDomainEventAsync(aggregateRootEvent).GetAwaiter().GetResult();
		}

		public virtual void AssertInvariantsAreMet()
		{
			if (AggregateRootId == Guid.Empty)
			{
				throw new InvariantsNotMetException($"{nameof(AggregateRootId)} not set.");
			}
		}

		private void ApplyEvent(TAggregateRootEventInterface aggregateRootEvent)
		{
			if (aggregateRootEvent is IAggregateRootCreatedEvent)
			{
				SetId(aggregateRootEvent.AggregateRootId);
			}
			Version = aggregateRootEvent.Version;
			_eventDispatcher.Dispatch(aggregateRootEvent);
		}

		protected EventDispatcher<TAggregateRootEventInterface>.RegistrationBuilder RegisterEventAppliers()
		{
			return _eventDispatcher.RegisterHandlers();
		}

		#region IEventStored

		private IDomainEventController _eventController;

		public void Initialize(IDomainEventController eventController, IDomainEvent[] eventStream,
			ITimeProvider timeProvider = null)
		{
			Initialize(eventController, timeProvider);

			if (eventStream == null)
				return;

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

		#endregion
	}
}