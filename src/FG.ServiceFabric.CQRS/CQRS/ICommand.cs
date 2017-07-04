using System;

namespace FG.ServiceFabric.CQRS
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