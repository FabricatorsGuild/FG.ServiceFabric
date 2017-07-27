using System;
using System.Fabric;
using System.Threading;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.DocumentDb;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                ActorRuntime.RegisterActorAsync<DbStoredActor>(
                   (context, actorType) => new DbStoredActorService(
                       context, 
                       actorType,
                       stateProvider: new DocumentDbActorStateProvider(new CosmosDbStateSession(new DatabaseSettingsProvider(context)), context, actorType),
                       actorFactory: (actorService, actorId) =>
                                              new DbStoredActor(
                                                  actorService,
                                                  actorId))).GetAwaiter().GetResult();
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
