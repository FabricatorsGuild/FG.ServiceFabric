using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
	public class ActorDemoActorService : FG.ServiceFabric.Actors.Runtime.ActorService, IActorDemoActorService
	{
		private int _count = 0;

		public ActorDemoActorService(
			StatefulServiceContext context,
			ActorTypeInformation actorTypeInfo,
			Func<ActorService, ActorId, Actors.Runtime.ActorBase> actorFactory = null,
			Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory =
				null,
			IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) :
			base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
		{
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

		public async Task<IEnumerable<string>> GetActorsAsync(CancellationToken cancellationToken)
		{
			var results = new List<string>();
			var pagedResult = await StateProvider.GetActorsAsync(100000, null, cancellationToken);
			foreach (var item in pagedResult.Items)
			{
				results.Add(item.GetStringId());
			}
			return results;
		}

		public Task<IEnumerable<string>> GetStoredStates(ActorId actorId, CancellationToken cancellationToken)
		{
			return this.StateProvider.EnumerateStateNamesAsync(actorId, cancellationToken);
		}


		public async Task RemoveAsync(ActorId actorId, CancellationToken cancellationToken)
		{
			await StateProvider.RemoveActorAsync(actorId, cancellationToken);
		}

		public Task<int> GetInternalCount(CancellationToken cancellationToken)
		{
			return Task.FromResult(_count);
		}

		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			foreach (var serviceReplicaListener in base.CreateServiceReplicaListeners())
			{
				yield return serviceReplicaListener;
			}
		}

		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			while (true)
			{
				_count++;

				await Task.Delay(100, cancellationToken);
			}
		}
	}

	public interface IActorDemoActorService : IActorService
	{
		Task<int> GetInternalCount(CancellationToken cancellationToken);
		Task<int> GetCountAsync(ActorId id, CancellationToken cancellationToken);
		Task<IEnumerable<int>> GetCountsAsync(CancellationToken cancellationToken);
		Task RemoveAsync(ActorId actorId, CancellationToken cancellationToken);

		Task<IEnumerable<string>> GetActorsAsync(CancellationToken cancellationToken);

		Task<IEnumerable<string>> GetStoredStates(ActorId actorId, CancellationToken cancellationToken);
	}
}