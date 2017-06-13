using System;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Actors.Remoting.Runtime
{
    public static class ServiceRemotingMessageHeadersExtensions
    {
        internal static ActorMessageHeaders GetActorMessageHeaders(this ServiceRemotingMessageHeaders messageHeaders)
        {
            try
            {
                ActorMessageHeaders actorMessageHeaders;
                if (ActorMessageHeaders.TryFromServiceMessageHeaders(messageHeaders, out actorMessageHeaders))
                {
                    return actorMessageHeaders;
                }
            }
            catch (Exception)
            {
                // ignored
            }
            return null;
        }
    }
}