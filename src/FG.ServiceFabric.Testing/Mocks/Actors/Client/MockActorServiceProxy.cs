namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    using System;

    using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

    public class MockActorServiceProxy<TActorServiceInterface> : MockServiceProxy<TActorServiceInterface>
        where TActorServiceInterface : IService
    {
        public MockActorServiceProxy(
            TActorServiceInterface target,
            Uri serviceUri,
            Type serviceInterfaceType,
            ServicePartitionKey partitionKey,
            TargetReplicaSelector replicaSelector,
            string listenerName,
            ICommunicationClientFactory<IServiceRemotingClient> factory,
            IMockServiceProxyManager serviceProxyManager)
            : base(target, serviceUri, serviceInterfaceType, partitionKey, replicaSelector, listenerName, factory, serviceProxyManager)
        {
        }
    }
}