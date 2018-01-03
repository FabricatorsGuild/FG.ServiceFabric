using FG.ServiceFabric.Services.Runtime.StateSession.Metadata;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IActorValueMetadata : IValueMetadata
    {
        ActorId ActorId { get; }
    }
}