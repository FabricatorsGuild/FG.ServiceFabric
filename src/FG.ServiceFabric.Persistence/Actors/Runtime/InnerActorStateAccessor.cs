namespace FG.ServiceFabric.Actors.Runtime
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    internal struct InnerActorStateAccessor : IInnerActorState
    {
        private readonly ActorId actorId;

        private readonly IActorStateProvider innerActorStateProvider;

        public InnerActorStateAccessor(ActorId actorId, IActorStateProvider innerActorStateProvider)
        {
            this.actorId = actorId;
            this.innerActorStateProvider = innerActorStateProvider;
        }

        public Task<T> LoadStateAsync<T>(string stateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.innerActorStateProvider.LoadStateAsync<T>(this.actorId, stateName, cancellationToken);
        }

        public Task<IEnumerable<string>> EnumerateStateNamesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.innerActorStateProvider.EnumerateStateNamesAsync(this.actorId, cancellationToken);
        }
    }
}