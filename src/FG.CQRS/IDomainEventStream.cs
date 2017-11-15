namespace FG.CQRS
{
	public interface IDomainEventStream
	{
		IDomainEvent[] DomainEvents { get; }
		void Append(IDomainEvent domainEvent);
	}
}