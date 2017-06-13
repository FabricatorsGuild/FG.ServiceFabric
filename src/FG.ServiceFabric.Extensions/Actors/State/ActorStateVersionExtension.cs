using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.State
{
    public static class ActorStateVersionExtension
    {
        public static async Task<int> GetCurrentStateVersion(this IActorStateProvider stateProvider, ActorId actorId, string stateKey, string versionKey)
        {
            var hasState = await stateProvider.ContainsStateAsync(actorId, stateKey, CancellationToken.None);            
            if (!hasState) return -1;

            var hasVersion = await stateProvider.ContainsStateAsync(actorId, stateKey, CancellationToken.None);
            if (!hasVersion) return 0;

            var currentStateVersionValue = await stateProvider.LoadStateAsync<int>(actorId, versionKey, CancellationToken.None);
            return currentStateVersionValue;
        }
    }
}