using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Fabric;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;

namespace FG.ServiceFabric.Actors.Runtime
{
    public static class ActorServiceExtensions
    {
        public static async Task<IDictionary<ActorId, ActorInformation>> GetFromAllActors<TActorType>(IPartitionEnumerationManager partitionEnumerationManager, IActorProxyFactory actorProxyFactory, 
            Uri serviceUri, CancellationToken cancellationToken = default(CancellationToken), int maxResults = 100)
            where TActorType : IActor
        {
            var servicePartitionKeysAsync = await ServiceContextExtensions.GetServicePartitionKeysAsync(partitionEnumerationManager, serviceUri);
            var result = new Dictionary<ActorId, ActorInformation>();
            var results = 0;
            foreach (var partitionInformation in servicePartitionKeysAsync)
            {
                var actorServiceProxy = actorProxyFactory.CreateActorServiceProxy<IActorService>(serviceUri, partitionInformation.LowKey);

                ContinuationToken continuationToken = null;
                do
                {
                    var page = await actorServiceProxy.GetActorsAsync(continuationToken, cancellationToken);
                    foreach (var actor in page.Items)
                    {
                        result.Add(actor.ActorId, actor);
                        results++;
                    }
                    if (results >= maxResults) return result;
                    continuationToken = page.ContinuationToken;
                }
                while (continuationToken != null);
            }
            return result;
        }

        public static async Task<IDictionary<ActorId, TResult>> GetFromAllActors<TActorType, TResult>(IPartitionEnumerationManager partitionEnumerationManager, 
            IActorProxyFactory actorProxyFactory, Uri serviceUri, Func<TActorType, Task<KeyValuePair<ActorId, TResult>>> onEachActor, CancellationToken cancellationToken = default(CancellationToken))
            where TActorType : IActor
        {
            var servicePartitionKeysAsync = await ServiceContextExtensions.GetServicePartitionKeysAsync(partitionEnumerationManager, serviceUri);
            var activeActors = new List<ActorInformation>();
            foreach (var partitionInformation in servicePartitionKeysAsync)
            {
                var actorServiceProxy = actorProxyFactory.CreateActorServiceProxy<IActorService>(serviceUri, partitionInformation.LowKey);
				

                ContinuationToken continuationToken = null;
                do
                {

                    var page = await actorServiceProxy.GetActorsAsync(continuationToken, cancellationToken);
                    activeActors.AddRange(page.Items);

                    continuationToken = page.ContinuationToken;
                } while (continuationToken != null);
            }

            var tasks = activeActors.Select(activeActor => actorProxyFactory.CreateActorProxy<TActorType>(activeActor.ActorId)).Select(onEachActor).ToList();
            return (await Task.WhenAll(tasks)).ToDictionary(task => task.Key, task => task.Value);
        }

    }
}