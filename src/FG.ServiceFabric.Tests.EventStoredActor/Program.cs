using System;
using System.Threading;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.EventStoredActor
{
	internal static class Program
	{
		private static void Main()
		{
			try
			{
				ActorRuntime.RegisterActorAsync<EventStoredActor>(
					(context, actorType) => new EventStoredActorService(context, actorType)).GetAwaiter().GetResult();

				ActorRuntime.RegisterActorAsync<IndexActor>(
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