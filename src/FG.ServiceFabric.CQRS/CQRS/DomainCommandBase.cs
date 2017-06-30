using System.Runtime.Serialization;

namespace FG.ServiceFabric.CQRS
{
    [DataContract]
    public abstract class DomainCommandBase : CommandBase, IDomainCommand
    {
    }
}