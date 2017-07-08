using System.Collections.Generic;

namespace FG.ServiceFabric.CQRS
{
    public interface IEventStored
    {
        void Initialize(IDomainEventController eventController, ITimeProvider timeProvider = null);
        void Initialize(IDomainEventController eventController, IDomainEvent[] eventStream, ITimeProvider timeProvider = null);
        
        IEnumerable<IDomainEvent> GetChanges();
        void ClearChanges();
    }
}