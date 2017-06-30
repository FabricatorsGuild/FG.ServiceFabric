using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS
{
    public interface IDomainEventController
    {
        Task RaiseDomainEvent<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent;

        Task<TRequestValue> Request<TRequestValue, TDomainRequest>(TDomainRequest domainRequest) where TDomainRequest : IDomainRequest<TRequestValue>;

        Task StoreDomainEventAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent;
    }
}
