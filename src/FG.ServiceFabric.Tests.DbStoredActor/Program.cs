using System;
using System.Fabric;
using System.Threading;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.DocumentDb;
using FG.ServiceFabric.DocumentDb.CosmosDb;
using Microsoft.ServiceFabric.Actors.Runtime;
using ActorService = FG.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.DbStoredActor
{
	internal static class Program
	{
		/// <summary>
		///     This is the entry point of the service host process.
		/// </summary>
		private static void Main()
		{
			try
			{
				ActorRuntime.RegisterActorAsync<DbStoredActor>(
					(context, actorType) => new ActorService(
						context,
						actorType,
						actorFactory: (actorService, actorId) =>
							new DbStoredActor(
								actorService,
								actorId,
								() => new CosmosDbStateSession(new DatabaseSettingsProvider(context))))).GetAwaiter().GetResult();
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