using System.Threading.Tasks;

namespace FG.CQRS
{
    public interface IHandleDomainEvent<in TDomainEvent>
        where TDomainEvent : IDomainEvent
    {
        Task Handle(TDomainEvent domainEvent);
    }
}