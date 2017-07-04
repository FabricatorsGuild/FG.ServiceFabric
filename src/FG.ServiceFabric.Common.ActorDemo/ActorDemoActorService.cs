using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using ActorBase = FG.ServiceFabric.Actors.Runtime.ActorBase;

namespace FG.ServiceFabric.Tests.Actor
{
    public class ActorDemoActorService : FG.ServiceFabric.Actors.Runtime.ActorService, IActorDemoActorService
    {
        public ActorDemoActorService(
            StatefulServiceContext context, 
            ActorTypeInformation actorTypeInfo, 
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorId, ActorBase> actorFactory = null, 
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null, 
            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) : 
            base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            foreach (var serviceReplicaListener in base.CreateServiceReplicaListeners())
            {
                yield return serviceReplicaListener;
            }
        }
        
        public async Task<int> GetCountAsync(ActorId id, CancellationToken cancellationToken)
        {
            var count = await StateProvider.LoadStateAsync<int>(
                id, "count", cancellationToken);

            return count;
        }

        public async Task<IEnumerable<int>> GetCountsAsync(CancellationToken cancellationToken)
        {
            ContinuationToken continuationToken = null;

            var result = new List<int>();
            do
            {
                var page = await StateProvider.GetActorsAsync(10000, continuationToken, cancellationToken);

                foreach (var actorId in page.Items)
                { 
                    var state = await StateProvider.LoadStateAsync<int>(
                                     actorId: actorId,
                                     stateName: "count",
                                     cancellationToken: cancellationToken);
                    result.Add(state);
                }

                continuationToken = page.ContinuationToken;
            } while (continuationToken != null);

            return result;
        }

        public async Task RemoveAsync(ActorId actorId, CancellationToken cancellationToken)
        {
            await StateProvider.RemoveActorAsync(actorId, cancellationToken);
        }
    }

    public interface IActorDemoActorService : IActorService
    {
        Task<int> GetCountAsync(ActorId id, CancellationToken cancellationToken);
        Task<IEnumerable<int>> GetCountsAsync(CancellationToken cancellationToken);
        Task RemoveAsync(ActorId actorId, CancellationToken cancellationToken);
    }
}