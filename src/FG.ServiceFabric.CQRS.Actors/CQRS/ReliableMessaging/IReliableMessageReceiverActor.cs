using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.CQRS.ReliableMessaging
{
    public interface IReliableMessageReceiverActor : IActor
    {
        Task ReceiveMessageAsync(ReliableMessage message);
    }
}