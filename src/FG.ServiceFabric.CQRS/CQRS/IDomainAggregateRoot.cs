namespace FG.ServiceFabric.CQRS
{
    public interface IEventStored
    {
        void Initialize(IDomainEventController domainEventController, ITimeProvider timeProvider = null);
        void Initialize(IDomainEventController domainEventController, IDomainEvent[] eventStream, ITimeProvider timeProvider = null);
    }
}