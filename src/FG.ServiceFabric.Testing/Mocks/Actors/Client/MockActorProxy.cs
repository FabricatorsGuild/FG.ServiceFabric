namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    using System;
    using System.Fabric;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Remoting.V1.Client;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

    using Serpent.InterfaceProxy;
    using Serpent.InterfaceProxy.Extensions;
    using Serpent.InterfaceProxy.Implementations.ProxyTypeBuilder;

    public class MockActorProxy<TActorInterface> : ActorProxy, IActorProxy
    {
        public MockActorProxy(
            TActorInterface target,
            Type actorInterfaceType,
            ActorId actorId,
            Uri serviceUri,
            ServicePartitionKey partitionKey,
            TargetReplicaSelector replicaSelector,
            string listenerName,
            ICommunicationClientFactory<IServiceRemotingClient> factory,
            IActorServicePartitionClient actorServicePartitionClient = null)
        {
            this.ActorId = actorId;
            this.ActorServicePartitionClient =
                actorServicePartitionClient ?? new MockActorServicePartitionClient(actorId, serviceUri, partitionKey, replicaSelector, listenerName, factory);

            this.Proxy = this.CreateDynamicProxy(target);
        }

        public new ActorId ActorId { get; }

        public new IActorServicePartitionClient ActorServicePartitionClient { get; }

        public new Microsoft.ServiceFabric.Actors.Remoting.V2.Client.IActorServicePartitionClient ActorServicePartitionClientV2 { get; }

        public TActorInterface Proxy { get; }

        private static Func<TActorInterface, IActorProxy, TActorInterface> ProxyFactory { get; } = CreateMockActorProxyFactory();

        protected override object GetReturnValue(int interfaceId, int methodId, object responseBody)
        {
            throw new NotImplementedException();
        }

        private static Func<TActorInterface, IActorProxy, TActorInterface> CreateMockActorProxyFactory()
        {
            var builder = (ProxyBuilder)ProxyBuilder
                .New
                .AddInterface(typeof(TActorInterface))
                .AddInterface(typeof(IActorProxy))
                .ParentType(typeof(BaseActorProxy<>).MakeGenericType(typeof(TActorInterface)))
                .TypeName("ActorProxy" + "_" + Guid.NewGuid().ToString("N"));

            return builder.Build().GetFactory<TActorInterface, IActorProxy, TActorInterface>();
        }

        private TActorInterface CreateDynamicProxy(TActorInterface target)
        {
            return ProxyFactory(target, this);
        }

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
                this.ActorId = actorId;
                this.ServiceUri = serviceUri;
                this.PartitionKey = partitionKey;
                this.TargetReplicaSelector = replicaSelector;
                this.ListenerName = listenerName;
                this.Factory = factory;
            }

            public ActorId ActorId { get; }

            public ICommunicationClientFactory<IServiceRemotingClient> Factory { get; }

            public string ListenerName { get; }

            public ServicePartitionKey PartitionKey { get; }

            public Uri ServiceUri { get; }

            public TargetReplicaSelector TargetReplicaSelector { get; }

            public bool TryGetLastResolvedServicePartition(out ResolvedServicePartition resolvedServicePartition)
            {
                throw new NotImplementedException();
            }
        }
    }
}