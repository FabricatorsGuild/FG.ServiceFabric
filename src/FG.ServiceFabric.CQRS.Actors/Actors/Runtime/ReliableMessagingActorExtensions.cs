using System.Threading.Tasks;
using FG.ServiceFabric.CQRS;
using FG.ServiceFabric.Services.Remoting;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    public static class ReliableMessageSenderExtensions
    {
        public static Task SendMessageAsync(this Microsoft.ServiceFabric.Actors.Runtime.ActorBase @this, ReliableMessage message, ActorReference actorReference)
        {
            return Task.FromResult(true);
        }

        public static Task SendMessageAsync(this Microsoft.ServiceFabric.Actors.Runtime.ActorBase @this, ReliableMessage message, ServiceReference serviceReference)
        {
            return Task.FromResult(true);
        }
    }
}
