using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS.ReliableMessaging
{
    public interface IReliableMessageHandler
    {
        Task ReceiveAsync<TMessage>(TMessage message);
    }
}