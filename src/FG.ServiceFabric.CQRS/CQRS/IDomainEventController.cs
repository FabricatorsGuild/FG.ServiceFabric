using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS
{
    public interface IDomainEventController
    {
        Task RaiseDomainEvent<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent;
    }
}
