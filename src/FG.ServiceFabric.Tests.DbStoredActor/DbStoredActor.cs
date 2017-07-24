using System;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.DbStoredActor.Interfaces;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class DbStoredActor : Actor, IDbStoredActor
    {
        private readonly Func<IDocumentDbStateWriter> _stateWriterFactory;
        private IDocumentDbStateWriter _stateWriter;
        private const string StateName = "count";
        
        public DbStoredActor(ActorService actorService, ActorId actorId, Func<IDocumentDbStateWriter> stateWriterFactory)
            : base(actorService, actorId)
        {
            _stateWriterFactory = stateWriterFactory;
        }
        
        protected override Task OnDeactivateAsync()
        {
            _stateWriter.Dispose();
            return base.OnDeactivateAsync();
        }

        protected override Task OnActivateAsync()
        {
            _stateWriter = _stateWriterFactory();
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");
            return StateManager.TryAddStateAsync(StateName, new CountState(this.GetActorId().ToString()));
        }
        
        Task<CountState> IDbStoredActor.GetCountAsync(CancellationToken cancellationToken)
        {
            return StateManager.GetStateAsync<CountState>(StateName, cancellationToken);
        }

        async Task IDbStoredActor.SetCountAsync(int count, CancellationToken cancellationToken)
        {
            var state = await StateManager.GetStateAsync<CountState>(StateName, cancellationToken);
            state = state.UpdateCount(count);
            
            await _stateWriter.UpsertAsync(state, StateName);
            await StateManager.AddOrUpdateStateAsync(StateName, state, (key, value) => state, cancellationToken);
        }
    }
}
