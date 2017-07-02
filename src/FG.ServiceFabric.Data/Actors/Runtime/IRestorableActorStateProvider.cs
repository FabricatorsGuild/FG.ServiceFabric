using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    public interface IRestorableActorStateProvider
    {
        Task<bool> HasRestorableStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
        Task RestoreStateAsync(ActorId actorId, string stateName, CancellationToken cancellationToken = new CancellationToken());
    }
}