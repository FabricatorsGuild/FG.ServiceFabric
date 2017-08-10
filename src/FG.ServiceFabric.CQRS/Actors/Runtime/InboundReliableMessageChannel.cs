using System.Threading.Tasks;

namespace FG.ServiceFabric.Actors.Runtime
{
    public class InboundReliableMessageChannel<TMessageBase> : IInboundReliableMessageChannel
    {
        private readonly IReliableMessageEndpoint<TMessageBase> _endpoint;

        public InboundReliableMessageChannel(IReliableMessageEndpoint<TMessageBase> endpoint)
        {
            _endpoint = endpoint;
        }

        public Task ReceiveMessageAsync(ReliableMessage message)
        {
            // Resolves type at runtime. 
            dynamic deserializedMessage = message.Deserialize();
            return _endpoint.HandleMessageAsync(deserializedMessage);
        }
    }
}

