using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors
{
    public interface IInboundReliableMessageChannel
    {
        Task ReceiveMessageAsync(ReliableMessage message);
    }
}