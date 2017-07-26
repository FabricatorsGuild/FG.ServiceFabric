using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Diagnostics;

namespace FG.ServiceFabric.Tests.PersonActor.Diagnostics
{
    public interface IActorLogger : IOutboundMessageChannelLogger
    {
    }
}
