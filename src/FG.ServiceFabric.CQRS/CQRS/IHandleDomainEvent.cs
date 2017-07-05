using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS
{
    public interface IHandleDomainEvent<in TDomainEvent>
        where TDomainEvent : IDomainEvent
    {
        Task Handle(TDomainEvent domainEvent);
    }
}