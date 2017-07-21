using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors
{
    public interface IReliableMessageReceiverActor : IActor
    {
        Task ReceiveMessageAsync(ReliableMessage message);
    }
    
}