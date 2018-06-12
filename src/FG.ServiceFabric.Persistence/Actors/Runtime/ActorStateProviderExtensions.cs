namespace FG.ServiceFabric.Actors.Runtime
{
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;

    internal static class ActorStateProviderExtensions
    {
        public static async Task<ConditionalValue<T>> TryGetState<T>(this IActorStateProvider actorStateProvider, ActorId actorId, string stateName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (await actorStateProvider.ContainsStateAsync(actorId, stateName, cancellationToken))
            {
                return new ConditionalValue<T>(true, await actorStateProvider.LoadStateAsync<T>(actorId, stateName, cancellationToken));
            }

            return new ConditionalValue<T>();
        }

        public static async Task SetStateValueAsync<T>(this IActorStateProvider actorStateProvider, ActorId actorId, string stateName, T value, CancellationToken cancellationToken = default(CancellationToken))
        {

            var change = (await actorStateProvider.ContainsStateAsync(actorId, stateName, cancellationToken)) ? StateChangeKind.Update : StateChangeKind.Add;
            await actorStateProvider.SaveStateAsync(actorId, new[] { new ActorStateChange(stateName, typeof(T), value, change) }, cancellationToken);
        }
    }
}