using System;
using FG.ServiceFabric.Actors.Remoting.Runtime;
using FG.ServiceFabric.Services.Remoting.FabricTransport;

namespace FG.ServiceFabric.Diagnostics
{
    public interface IActorClientLogger : IServiceClientLogger
    {
        IDisposable CallActor(Uri requestUri, string actorMethodName, ActorMessageHeaders actorMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader);

        void CallActorFailed(Uri requestUri, string actorMethodName, ActorMessageHeaders actorMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader, Exception ex);

    }
}