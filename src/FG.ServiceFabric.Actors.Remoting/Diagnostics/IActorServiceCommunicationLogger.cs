using System;
using FG.ServiceFabric.Actors.Remoting.Runtime;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.V1;

namespace FG.ServiceFabric.Diagnostics
{
    public interface IActorServiceCommunicationLogger : IServiceCommunicationLogger
    {
        IDisposable RecieveActorMessage(Uri requestUri, string actorMethodName, ActorMessageHeaders actorMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader);

        void RecieveActorMessageFailed(Uri requestUri, string actorMethodName, ActorMessageHeaders actorMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader,
            Exception ex);

        void FailedToGetActorMethodName(ActorMessageHeaders actorMessageHeaders, Exception ex);

        void FailedToReadActorMessageHeaders(ServiceRemotingMessageHeaders serviceRemotingMessageHeaders, Exception ex);        
    }
}