using FG.ServiceFabric.Diagnostics;

namespace FG.ServiceFabric.Tests.EventStoredActor.Diagnostics
{
    public interface IActorLogger : IOutboundMessageChannelLogger
    {
    }
}