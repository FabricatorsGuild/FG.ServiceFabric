using System;
using System.Fabric;
using FG.ServiceFabric.Diagnostics.Tracing;
using FG.ServiceFabric.Services.Diagnostics.Tracing;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Actors.Diagnostics.Tracing
{
    public interface IActorEventSource
    {
        void ActorMessage(Actor actor, string message);
        //void ActorWarning(Actor actor, string message);
        void ActorError(Actor actor, string message, Exception ex);
    }
}