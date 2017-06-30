using System.Runtime.Serialization;

namespace FG.ServiceFabric.CQRS
{
    [DataContract]
    public abstract class ServiceCommandBase : CommandBase, IServiceCommand
    {
        protected ServiceCommandBase() : base()
        {
        }
    }
}