namespace FG.ServiceFabric.Actors.Runtime.SaveStateHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using FG.ServiceFabric.Actors.Runtime.RuntimeInformation;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public class ReliableStateSaver : ISaveReliableState
    {
        public async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public async Task SaveStateAsync(
            ActorId actorId,
            ActorRuntimeInformation actorRuntimeInformation,
            IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}