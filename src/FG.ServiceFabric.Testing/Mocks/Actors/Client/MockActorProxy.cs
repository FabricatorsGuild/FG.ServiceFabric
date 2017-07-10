using System;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
	public class MockActorProxy : IActorProxy
	{
		public MockActorProxy(
			ActorId actorId,
			Uri serviceUri,
			ServicePartitionKey partitionKey,
			TargetReplicaSelector replicaSelector,
			string listenerName,
			ICommunicationClientFactory<IServiceRemotingClient> factory)
		{
			ActorId = actorId;
			ActorServicePartitionClient = new MockActorServicePartitionClient(actorId, serviceUri, partitionKey, replicaSelector, listenerName, factory);
		}

		public ActorId ActorId { get; }
		public IActorServicePartitionClient ActorServicePartitionClient { get; }

		private class MockActorServicePartitionClient : IActorServicePartitionClient
		{
			internal MockActorServicePartitionClient(
				ActorId actorId,
				Uri serviceUri,
				ServicePartitionKey partitionKey,
				TargetReplicaSelector replicaSelector,
				string listenerName,
				ICommunicationClientFactory<IServiceRemotingClient> factory)
			{
				ActorId = actorId;
				ServiceUri = serviceUri;
				PartitionKey = partitionKey;
				TargetReplicaSelector = replicaSelector;
				ListenerName = listenerName;
				Factory = factory;
			}

			public bool TryGetLastResolvedServicePartition(out ResolvedServicePartition resolvedServicePartition)
			{
				throw new NotImplementedException();
			}

			public Uri ServiceUri { get; }
			public ServicePartitionKey PartitionKey { get; }
			public TargetReplicaSelector TargetReplicaSelector { get; }
			public string ListenerName { get; }
			public ICommunicationClientFactory<IServiceRemotingClient> Factory { get; }
			public ActorId ActorId { get; }
		}
	}
}