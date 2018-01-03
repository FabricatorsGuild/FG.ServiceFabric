using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.State
{
    public abstract class MigrationBase<TState, TPredecessor>
    {
        private readonly int _version;

        protected MigrationBase(int version)
        {
            _version = version;
        }

        protected abstract TState MigrateUp(TPredecessor predecessor);

        protected abstract Task<TPredecessor> MigratePredecessor(int currentStateVersion,
            IActorStateProvider stateProvider,
            ActorId actorId, string stateKey, string versionKey);

        public async Task<TState> Migrate(int currentStateVersion, IActorStateProvider stateProvider, ActorId actorId,
            string stateKey, string versionKey)
        {
            if (currentStateVersion == _version)
            {
                if (await stateProvider.ContainsStateAsync(actorId, stateKey, CancellationToken.None))
                {
                    var state = await stateProvider.LoadStateAsync<TState>(actorId, stateKey, CancellationToken.None);
                    return state;
                }
            }
            else if (currentStateVersion < _version)
            {
                var oldState =
                    await MigratePredecessor(currentStateVersion, stateProvider, actorId, stateKey, versionKey);
                var state = MigrateUp(oldState);

                await stateProvider.SaveStateAsync(actorId,
                    new[] {new ActorStateChange(stateKey, typeof(TState), state, StateChangeKind.Update)},
                    CancellationToken.None);
                return state;
            }
            await stateProvider.SaveStateAsync(actorId,
                new[] {new ActorStateChange(stateKey, typeof(int), _version, StateChangeKind.Update)},
                CancellationToken.None);
            return default(TState);
        }
    }
}