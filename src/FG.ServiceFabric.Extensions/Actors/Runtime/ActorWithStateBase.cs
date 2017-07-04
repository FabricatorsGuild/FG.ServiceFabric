using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace FG.ServiceFabric.Actors.Runtime
{
    public abstract class ActorWithStateBase : ActorBase
    {
        private const string CoreStateName = @"core_state";

        protected ActorWithStateBase(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
        }

        protected async Task<T> GetCoreState<T>(CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await this.StateManager.TryGetStateAsync<T>(CoreStateName, cancellationToken);
            if (state.HasValue)
            {
                return state.Value;
            }
            return default(T);
        }

        protected async Task SetCoreState<T>(T state, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.StateManager.SetStateAsync(CoreStateName, state, cancellationToken);
        }
    }
}