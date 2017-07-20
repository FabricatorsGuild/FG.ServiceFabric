namespace FG.CQRS
{
    public interface IEventStored
    {
        void Initialize(IDomainEventController eventController, ITimeProvider timeProvider = null);
        void Initialize(IDomainEventController eventController, IDomainEvent[] eventStream, ITimeProvider timeProvider = null);
    }
}