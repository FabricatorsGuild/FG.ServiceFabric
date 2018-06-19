namespace FG.ServiceFabric.Actors.Runtime
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    public interface IOverloadedStateSessionActorStateProvider : IActorStateProvider, IQueryableActorStateProvider
    {
        Task UpdateActorInnerStateAsync(ActorId actorId, CancellationToken cancellationToken);
    }
}