using System;

namespace FG.CQRS
{
	public interface IDomainEvent
	{
		Guid EventId { get; }
		DateTime UtcTimeStamp { get; set; }
	}
}