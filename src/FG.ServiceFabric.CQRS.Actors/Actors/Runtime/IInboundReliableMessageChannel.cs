using System.Threading.Tasks;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IInboundReliableMessageChannel
    {
        Task ReceiveMessageAsync(ReliableMessage message);
    }
}