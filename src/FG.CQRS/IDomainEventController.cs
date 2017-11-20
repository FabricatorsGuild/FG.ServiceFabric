using System.Threading.Tasks;

namespace FG.CQRS
{
	public interface IDomainEventController
	{
		Task RaiseDomainEventAsync<TDomainEvent>(TDomainEvent domainEvent) where TDomainEvent : IDomainEvent;
	}
}