namespace FG.ServiceFabric.Actors.Runtime.SaveStateHandlers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public interface ISaveReliableState
    {
        Task SaveStateAsync(ActorId actorId, ActorRuntimeInformation actorRuntimeInformation, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken = default(CancellationToken));
    }
}