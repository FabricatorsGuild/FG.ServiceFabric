using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.State
{
    public class MigrationWithFunctions<T, TPredecessor> : MigrationBase<T, TPredecessor>
    {
        private readonly Func<TPredecessor, T> _migrateUp;
        private readonly Func<int, IActorStateProvider, ActorId, string, string, Task<TPredecessor>> _migratePredecessorStateProvider;

        public MigrationWithFunctions(
            int version, 
            Func<TPredecessor, T> migrateUp,
            Func<int, IActorStateProvider, ActorId, string, string, Task<TPredecessor>> migratePredecessorStateProvider = null
            ) : base(version)
        {
            _migrateUp = migrateUp;
            _migratePredecessorStateProvider = migratePredecessorStateProvider;
        }

        protected override T MigrateUp(TPredecessor predecessor)
        {
            return _migrateUp(predecessor);
        }

        protected override Task<TPredecessor> MigratePredecessor(int currentStateVersion, IActorStateProvider stateProvider, 
            ActorId actorId, string stateKey, string versionKey)
        {
            return _migratePredecessorStateProvider(currentStateVersion, stateProvider, actorId, stateKey, versionKey);
        }
    }
}