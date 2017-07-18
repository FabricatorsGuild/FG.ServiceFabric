using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.PersonActor
{
    internal static class Program
    {
        private static void Main()
        {
            try
            {
                ActorRuntime.RegisterActorAsync<PersonActor>(
                   (context, actorType) => new PersonActorService(context, actorType)).GetAwaiter().GetResult();

                //ActorRuntime.RegisterActorAsync<PersonActor>(
                //   (context, actorType) => new PersonActorService(context, actorType, settings:
                //        new ActorServiceSettings()
                //        {
                //            ActorGarbageCollectionSettings =
                //                new ActorGarbageCollectionSettings(idleTimeoutInSeconds: 15, scanIntervalInSeconds: 15)
                //        })).GetAwaiter().GetResult();

                ActorRuntime.RegisterActorAsync<PersonIndexActor>(
                   (context, actorType) => new ActorService(context, actorType)).GetAwaiter().GetResult();
                
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
