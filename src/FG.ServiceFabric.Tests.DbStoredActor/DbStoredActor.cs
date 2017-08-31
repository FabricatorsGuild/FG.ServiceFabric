using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Tests.DbStoredActor.Interfaces;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class DbStoredActor : Actors.Runtime.ActorBase, IDbStoredActor
    {
        private readonly Func<IDocumentDbStateWriter> _stateWriterFactory;
        private IDocumentDbStateWriter _stateWriter;
        private const string StateName = "count";
        private const string ReadModelName = "count_read";
        
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
        
        Task<CountReadModel> IDbStoredActor.GetCountAsync(CancellationToken cancellationToken)
        {
            return StateManager.GetStateAsync<CountReadModel>(ReadModelName, cancellationToken);
        }

        async Task IDbStoredActor.SetCountAsync(int count, CancellationToken cancellationToken)
        {
            var state = await StateManager.GetStateAsync<CountState>(StateName, cancellationToken);
            state = state.UpdateCount(count);
            await StateManager.AddOrUpdateStateAsync(StateName, state, (key, value) => state, cancellationToken);
            await _stateWriter.UpsertAsync(state, new StateMetadata(this.ActorService.Context, StateName));

            var readModel = new CountReadModel { Count = state.Count, Id = state.Id + "_readmodel"};
            await StateManager.AddOrUpdateStateAsync(ReadModelName, readModel, (key, value) => readModel, cancellationToken);
            await _stateWriter.UpsertAsync(readModel, new StateMetadata(this.ActorService.Context, ReadModelName));
        }
    }

    public class StateMetadata : IStateMetadata
    {
        public StateMetadata(ServiceContext context, string stateName)
        {
            PartitionId = context.PartitionId;
            StateName = stateName;
        }

        public string StateName { get; set; }
        public string PartitionKey { get; set; }
	    public Guid PartitionId { get; set; }		
    }

}
