using System;
using System.Configuration;
using System.Fabric;
using System.Threading;
using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.Fabric;
using FG.ServiceFabric.Services.Runtime.StateSession;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
	internal static class Program
	{
		private static void Main()
		{
			try
			{
				ActorRuntime.RegisterActorAsync<WithInteralError.ActorDemo>(
						(context, actorType) => new ActorDemoActorService(context, actorType,
							stateProvider: new StateSessionActorStateProvider(context, CreateStateManager(context), actorType))).GetAwaiter()
					.GetResult();

				Thread.Sleep(Timeout.Infinite);
			}
			catch (Exception e)
			{
				ActorDemoEventSource.Current.ActorHostInitializationFailed(e.ToString());
				throw;
			}
		}

		private static IStateSessionManager CreateStateManager(StatefulServiceContext context)
		{
			return new InMemoryStateSessionManager(
				StateSessionHelper.GetServiceName(context.ServiceName),
				context.PartitionId,
				StateSessionHelper.GetPartitionInfo(context,
					() => new FabricClientQueryManagerPartitionEnumerationManager(new FabricClient())).GetAwaiter().GetResult());
		}
	}
}