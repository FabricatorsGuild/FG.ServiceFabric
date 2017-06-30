using System;

namespace FG.ServiceFabric.CQRS
{
    public interface IEventStoreSession
    {
        TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored;
        void SaveChanges();
        void Delete(Guid aggregateId);
    }
}
