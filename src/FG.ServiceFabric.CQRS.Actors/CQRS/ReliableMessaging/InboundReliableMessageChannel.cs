using System.Threading.Tasks;

namespace FG.ServiceFabric.CQRS.ReliableMessaging
{
    public class InboundReliableMessageChannel : IInboundReliableMessageChannel
    {
        private readonly IReliableMessageHandler _handler;

        public InboundReliableMessageChannel(IReliableMessageHandler handler)
        {
            _handler = handler;
        }

        public Task ReceiveAsync(ReliableMessage message)
        {
            dynamic deserializedMessage = message.Deserialize();
            return _handler.ReceiveAsync(deserializedMessage);
        }
    }
}