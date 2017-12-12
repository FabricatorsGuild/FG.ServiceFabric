using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using FG.ServiceFabric.Actors.Remoting.Runtime;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Actors.Runtime
{
	public class ProcessOnlyOnceManager : IProcessOnlyOnceActorService
	{
		private readonly Microsoft.ServiceFabric.Actors.Runtime.ActorService _actorService;

		public ProcessOnlyOnceManager(Microsoft.ServiceFabric.Actors.Runtime.ActorService actorService)
		{
			_actorService = actorService;
		}

		public byte[] GetProcessedResult(ActorId actorId, Guid commandId)
		{
			try
			{
				var processedResult = _actorService.StateProvider
					.LoadStateAsync<byte[]>(actorId, $"fg-handled-commands-{commandId}", CancellationToken.None).GetAwaiter().GetResult();
				return processedResult;
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to load command state for {actorId} + {commandId}", ex);
			}
		}

		public void StoreProcessedResult(ActorId actorId, Guid commandId, byte[] result)
		{
			try
			{
				var actorStateChanges = new ActorStateChange[]
					{new ActorStateChange($"fg-handled-commands-{commandId}", typeof(byte[]), result, StateChangeKind.Add)};
				_actorService.StateProvider
					.SaveStateAsync(actorId, actorStateChanges, CancellationToken.None).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to save command state for {actorId} + {commandId}", ex);
			}
		}
	}

	public class ActorService : Microsoft.ServiceFabric.Actors.Runtime.ActorService
	{
		private IActorProxyFactory _actorProxyFactory;
		private ApplicationUriBuilder _applicationUriBuilder;
		private IServiceProxyFactory _serviceProxyFactory;

		public ActorService(
			StatefulServiceContext context,
			ActorTypeInformation actorTypeInfo,
			Func<Microsoft.ServiceFabric.Actors.Runtime.ActorService, ActorId, ActorBase> actorFactory = null,
			Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory =
				null,
			IActorStateProvider stateProvider = null,
			ActorServiceSettings settings = null,
			IReliableStateManagerReplica reliableStateManagerReplica = null) :
			base(context, actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
		{
			StateManager = reliableStateManagerReplica ??
			               (IReliableStateManagerReplica) new ReliableStateManager(context,
				               (ReliableStateManagerConfiguration) null);
		}


		public IReliableStateManager StateManager { get; private set; }

		public ApplicationUriBuilder ApplicationUriBuilder =>
			_applicationUriBuilder ?? (_applicationUriBuilder = new ApplicationUriBuilder());

		public IActorProxyFactory ActorProxyFactory => _actorProxyFactory ?? (_actorProxyFactory = new ActorProxyFactory());

		public IServiceProxyFactory ServiceProxyFactory =>
			_serviceProxyFactory ?? (_serviceProxyFactory = new ServiceProxyFactory());

		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			return base.CreateServiceReplicaListeners();
		}
	}
}