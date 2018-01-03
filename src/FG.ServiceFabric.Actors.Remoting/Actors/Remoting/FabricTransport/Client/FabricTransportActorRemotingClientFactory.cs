using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.Builder;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport.Client
{
    public class FabricTransportActorRemotingClientFactory : IServiceRemotingClientFactory,
        ICommunicationClientFactory<IServiceRemotingClient>
    {
        private readonly ICommunicationClientFactory<IServiceRemotingClient> _innerClientFactory;
        private readonly IActorClientLogger _logger;
        private readonly MethodDispatcherBase[] _serviceMethodDispatchers;

        public FabricTransportActorRemotingClientFactory(
            ICommunicationClientFactory<IServiceRemotingClient> innerClientFactory,
            IActorClientLogger logger,
            MethodDispatcherBase[] serviceMethodDispatchers
        )
        {
            _innerClientFactory = innerClientFactory;
            _logger = logger;
            _serviceMethodDispatchers = serviceMethodDispatchers;
            _innerClientFactory.ClientConnected += OnClientConnected;
            _innerClientFactory.ClientDisconnected += OnClientDisconnected;
        }

        public async Task<IServiceRemotingClient> GetClientAsync(Uri serviceUri, ServicePartitionKey partitionKey,
            TargetReplicaSelector targetReplicaSelector, string listenerName,
            OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            var client = await _innerClientFactory.GetClientAsync(
                serviceUri,
                partitionKey,
                targetReplicaSelector,
                listenerName,
                retrySettings,
                cancellationToken);
            return new FabricTransportActorRemotingClient(client, serviceUri, _logger, _serviceMethodDispatchers);
        }

        public async Task<IServiceRemotingClient> GetClientAsync(ResolvedServicePartition previousRsp,
            TargetReplicaSelector targetReplicaSelector, string listenerName, OperationRetrySettings retrySettings,
            CancellationToken cancellationToken)
        {
            var client = await _innerClientFactory.GetClientAsync(
                previousRsp,
                targetReplicaSelector,
                listenerName,
                retrySettings,
                cancellationToken);
            return new FabricTransportActorRemotingClient(client, previousRsp.ServiceName, _logger,
                _serviceMethodDispatchers);
        }

        public Task<OperationRetryControl> ReportOperationExceptionAsync(IServiceRemotingClient client,
            ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings,
            CancellationToken cancellationToken)
        {
            var fabricTransportActorRemotingClient = client as FabricTransportActorRemotingClient;
            _logger?.ServiceClientFailed(
                fabricTransportActorRemotingClient?.ResolvedServicePartition.ServiceName,
                ServiceRequestContext.Current?.GetCustomHeader(),
                exceptionInformation.Exception);
            return _innerClientFactory.ReportOperationExceptionAsync(
                fabricTransportActorRemotingClient?.InnerClient,
                exceptionInformation,
                retrySettings,
                cancellationToken);
        }

        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientConnected;
        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientDisconnected;

        private void OnClientConnected(object sender, CommunicationClientEventArgs<IServiceRemotingClient> e)
        {
            ClientConnected?.Invoke(this, new CommunicationClientEventArgs<IServiceRemotingClient>
            {
                Client = e.Client
            });
        }

        private void OnClientDisconnected(object sender, CommunicationClientEventArgs<IServiceRemotingClient> e)
        {
            ClientDisconnected?.Invoke(this, new CommunicationClientEventArgs<IServiceRemotingClient>
            {
                Client = e.Client
            });
        }
    }
}