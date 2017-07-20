using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IOutboundReliableMessageChannel
    {
        Task ProcessQueueAsync();
        Task SendMessageAsync<TActorInterface>(ReliableMessage message, ActorId actorId, string applicationName = null,
            string serviceName = null, string listerName = null)
            where TActorInterface : IReliableMessageReceiverActor;
    }
}