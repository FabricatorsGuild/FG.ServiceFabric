namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    using System;
    using System.Fabric;

    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

    using Serpent.InterfaceProxy.Extensions;
    using Serpent.InterfaceProxy.Implementations.ProxyTypeBuilder;

    public class MockServiceProxy<TServiceInterface> : IServiceProxy
        where TServiceInterface : IService
    {
        private readonly MockServiceInstanceProxyCache instanceProxyCache = new MockServiceInstanceProxyCache();

        public MockServiceProxy(
            TServiceInterface target,
            Uri serviceUri,
            Type serviceInterfaceType,
            ServicePartitionKey partitionKey,
            TargetReplicaSelector replicaSelector,
            string listenerName,
            ICommunicationClientFactory<IServiceRemotingClient> factory,
            IMockServiceProxyManager serviceProxyManager)
        {
            this.ServiceInterfaceType = serviceInterfaceType;
            this.ServicePartitionClient = new MockServicePartitionClient(serviceUri, partitionKey, replicaSelector, listenerName, factory);

            this.Proxy = this.instanceProxyCache.GetOrAdd(
                new MockServiceInstanceProxyCacheKey(serviceUri, serviceInterfaceType, partitionKey),
                k => this.CreateDynamicProxy(target));
        }

        public object Proxy { get; }

        public Type ServiceInterfaceType { get; }

        public IServiceRemotingPartitionClient ServicePartitionClient { get; }

        public Microsoft.ServiceFabric.Services.Remoting.V2.Client.IServiceRemotingPartitionClient ServicePartitionClient2 { get; }

        private static Func<TServiceInterface, IServiceProxy, TServiceInterface> ProxyFactory { get; } = CreateServiceProxyFactory();

        protected TServiceInterface CreateDynamicProxy(TServiceInterface target)
        {
            return ProxyFactory(target, this);
        }

        private static Func<TServiceInterface, IServiceProxy, TServiceInterface> CreateServiceProxyFactory()
        {
            var builder = (ProxyBuilder)ProxyBuilder.New.AddInterface(typeof(TServiceInterface))
                .AddInterface(typeof(IServiceProxy))
                .ParentType(typeof(BaseServiceProxy<>).MakeGenericType(typeof(TServiceInterface)))
                .TypeName("ServiceProxy" + "_" + Guid.NewGuid().ToString("N"));

            return builder.Build().GetFactory<TServiceInterface, IServiceProxy, TServiceInterface>();
        }

        private class MockServicePartitionClient : IServiceRemotingPartitionClient
        {
            internal MockServicePartitionClient(
                Uri serviceUri,
                ServicePartitionKey partitionKey,
                TargetReplicaSelector replicaSelector,
                string listenerName,
                ICommunicationClientFactory<IServiceRemotingClient> factory)
            {
                this.ServiceUri = serviceUri;
                this.PartitionKey = partitionKey;
                this.TargetReplicaSelector = replicaSelector;
                this.ListenerName = listenerName;
                this.Factory = factory;
            }

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