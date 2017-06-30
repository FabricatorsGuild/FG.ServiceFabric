using System;

namespace FG.ServiceFabric.CQRS
{
    public interface ICommand
    {
        Guid CommandId { get; }
    }
}