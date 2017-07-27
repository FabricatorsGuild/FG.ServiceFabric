using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.DocumentDb;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Actors.Runtime
{
    public partial class DocumentDbActorStateProvider : WrappedActorStateProvider, IDisposable
    {
        private readonly IDocumentDbStateWriter _documentDb;
        private readonly ServiceContext _serviceContext;

        private readonly IDictionary<Type, object> _replicatedTypes = new ConcurrentDictionary<Type, object>();
        
        public DocumentDbActorStateProvider(IDocumentDbStateWriter documentDb, ServiceContext serviceContext, ActorTypeInformation actorTypeInfo = null, IActorStateProvider stateProvider = null) : base(stateProvider, actorTypeInfo)
        {
            _documentDb = documentDb;
            _serviceContext = serviceContext;
        }
        
        public ConfigurationBuilder Configure()
        {
            return new ConfigurationBuilder(this);
        }
        
        public override async Task SaveStateAsync(ActorId actorId, IReadOnlyCollection<ActorStateChange> stateChanges,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await ReplicateAsync(stateChanges);

            // Pass to backing state provider, it acts as a second storage, cache if so like.
            await base.SaveStateAsync(actorId, stateChanges, cancellationToken);
        }

        // TODO: Do this transctional? Or use queue instead?
        private Task ReplicateAsync(IEnumerable<ActorStateChange> stateChanges)
        {
            var replicatedStateChanges = stateChanges.Where(sc => _replicatedTypes.ContainsKey(sc.Type));

            var tasks = new List<Task>();
            foreach (var stateChange in replicatedStateChanges)
            {
                dynamic value = Convert.ChangeType(stateChange.Value, stateChange.Type);
                var metadata = new StateMetadata
                {
                    StateName = stateChange.StateName,
                    PartitionKey = _serviceContext.PartitionId
                };

                switch (stateChange.ChangeKind)
                {
                    case StateChangeKind.None:
                        break;
                    case StateChangeKind.Add:
                    case StateChangeKind.Update:
                        tasks.Add(_documentDb.UpsertAsync(value, metadata));
                        break;
                    case StateChangeKind.Remove:
                        tasks.Add(_documentDb.DelecteAsync(value.Id, metadata.PartitionKey));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return Task.WhenAll(tasks);
        }
        
        public void Dispose()
        {
            _documentDb?.Dispose();
        }
    }
}
