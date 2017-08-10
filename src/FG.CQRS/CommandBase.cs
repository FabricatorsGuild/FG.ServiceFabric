using System;
using System.Runtime.Serialization;

namespace FG.CQRS
{
    [DataContract]
    public abstract class CommandBase : ICommand
    {
        protected CommandBase()
        {
            CommandId = Guid.NewGuid();
        }

        [DataMember]
        public Guid CommandId { get; private set; }

    }

    [DataContract]
    public abstract class DomainCommandBase : CommandBase, IDomainCommand
    {
    }

    [DataContract]
    public abstract class ServiceCommandBase : CommandBase, IServiceCommand
    {
    }
}