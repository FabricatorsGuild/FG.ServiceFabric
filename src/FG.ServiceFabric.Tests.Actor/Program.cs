using System;
using System.Threading;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
    internal static class Program
    {
        private static void Main()
        {
            try
            {
                ActorRuntime.RegisterActorAsync<ActorDemo>(
                    (context, actorType) => new ActorDemoActorService(context, actorType)).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorDemoEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
