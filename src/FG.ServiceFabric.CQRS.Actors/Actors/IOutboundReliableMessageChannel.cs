using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors
{
    public interface IOutboundReliableMessageChannel
    {
        Task ProcessQueueAsync();
        Task SendMessageAsync<TActorInterface>(ReliableMessage message, ActorId actorId, string applicationName = null,
            string serviceName = null, string listerName = null)
            where TActorInterface : IReliableMessageReceiverActor;
    }
}