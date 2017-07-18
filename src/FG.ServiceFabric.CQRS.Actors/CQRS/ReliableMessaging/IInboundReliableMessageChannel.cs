using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS.ReliableMessaging
{
    public interface IInboundReliableMessageChannel
    {
        Task ReceiveAsync(ReliableMessage message);
    }
}