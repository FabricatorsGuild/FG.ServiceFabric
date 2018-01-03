using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.State
{
    public interface IMigrationContainer
    {
        int CurrentState();
        Task EnsureUpdated(IActorStateProvider stateProvider, ActorId actorId, string stateKey, string versionKey);
    }
}