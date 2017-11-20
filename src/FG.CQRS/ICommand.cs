using System;

namespace FG.CQRS
{
	public interface ICommand
	{
		Guid CommandId { get; }
	}

	public interface IDomainCommand : ICommand
	{
	}

	public interface IServiceCommand : ICommand
	{
	}
}