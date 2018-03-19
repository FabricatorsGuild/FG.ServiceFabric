using System;
using System.Collections.Concurrent;
using System.Fabric;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.Builder;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport.Client
{
    /// <summary>
    ///     Provides a service remoting client
    /// </summary>
    public class FabricTransportServiceRemotingClient : IServiceRemotingClient, ICommunicationClient
    {
        private static readonly ConcurrentDictionary<long, string> ServiceMethodMap =
            new ConcurrentDictionary<long, string>();

        private readonly IServiceClientLogger _logger;

        private readonly MethodDispatcherBase[] _serviceMethodDispatchers;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FabricTransportServiceRemotingClient" /> class.
        /// </summary>
        /// <param name="innerClient">
        ///     The inner client
        /// </param>
        /// <param name="serviceUri">
        ///     The service uri
        /// </param>
        /// <param name="logger">
        ///     A service client logger
        /// </param>
        /// <param name="serviceMethodDispatchers">
        ///     The service metod dispatchers
        /// </param>
        public FabricTransportServiceRemotingClient(
            IServiceRemotingClient innerClient,
            Uri serviceUri,
            IServiceClientLogger logger,
            MethodDispatcherBase[] serviceMethodDispatchers)
        {
            InnerClient = innerClient;
            ServiceUri = serviceUri;
            _logger = logger;
            _serviceMethodDispatchers = serviceMethodDispatchers;
        }

        /// <summary>
        ///     The inner service remoting client
        /// </summary>
        protected internal IServiceRemotingClient InnerClient { get; }

        /// <summary>
        ///     Gets the service uri
        /// </summary>
        protected Uri ServiceUri { get; }

        /// <summary>
        ///     Gets or sets the endpoint to which the client is connected
        /// </summary>
        public ResolvedServiceEndpoint Endpoint
        {
            get => InnerClient.Endpoint;
            set => InnerClient.Endpoint = value;
        }

        /// <summary>
        ///     Gets or Sets the name of the listener in the replica or instance to which the client is connected to
        /// </summary>
        public string ListenerName
        {
            get => InnerClient.ListenerName;
            set => InnerClient.ListenerName = value;
        }

        /// <summary>
        ///     Gets or Sets the Resolved service partition which was used when this client was created.
        /// </summary>
        public ResolvedServicePartition ResolvedServicePartition
        {
            get => InnerClient.ResolvedServicePartition;
            set => InnerClient.ResolvedServicePartition = value;
        }

        Task<byte[]> IServiceRemotingClient.RequestResponseAsync(ServiceRemotingMessageHeaders messageHeaders,
            byte[] requestBody)
        {
            var customServiceRequestHeader = UpdateAndGetMessageHeaders(messageHeaders);
            return RequestServiceResponseAsync(messageHeaders, customServiceRequestHeader, requestBody);
        }

        void IServiceRemotingClient.SendOneWay(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            var customServiceRequestHeader = UpdateAndGetMessageHeaders(messageHeaders);

            SendServiceOneWay(messageHeaders, customServiceRequestHeader, requestBody);
        }

        /// <summary>
        ///     Finalizes an instance of the <see cref="FabricTransportServiceRemotingClient" /> class.
        /// </summary>
        ~FabricTransportServiceRemotingClient()
        {
            if (InnerClient == null)
                return;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var disposable = InnerClient as IDisposable;
            disposable?.Dispose();
        }

        /// <summary>
        ///     Gets the service method name by interface id and method id
        /// </summary>
        /// <param name="interfaceId">The interface id</param>
        /// <param name="methodId">The method id</param>
        /// <returns>The name of the method</returns>
        protected string GetServiceMethodName(int interfaceId, int methodId)
        {
            try
            {
                var lookup = HashUtil.Combine(interfaceId, methodId);

                return ServiceMethodMap.GetOrAdd(
                    lookup,
                    lu =>
                    {
                        foreach (var serviceMethodDispatcher in _serviceMethodDispatchers)
                        {
                            var methodName = serviceMethodDispatcher.GetMethodDispatcherMapName(interfaceId, methodId);
                            if (methodName != null)
                                return methodName;
                        }

                        return null;
                    });
            }
            catch (Exception)
            {
                // ignored
                // _logger?.FailedToGetActorMethodName(actorMessageHeaders, ex);
            }

            return null;
        }

        protected virtual Task<byte[]> RequestServiceResponseAsync(
            ServiceRemotingMessageHeaders messageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader,
            byte[] requestBody)
        {
            var methodName = GetServiceMethodName(messageHeaders);
            using (_logger?.CallService(ServiceUri, methodName, messageHeaders, customServiceRequestHeader) ??
                   new SafeDisposable())
            {
                try
                {
                    var result = InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger?.CallServiceFailed(ServiceUri, methodName, messageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }

        protected virtual Task<byte[]> SendServiceOneWay(ServiceRemotingMessageHeaders messageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var methodName = GetServiceMethodName(messageHeaders);
            using (_logger?.CallService(ServiceUri, methodName, messageHeaders, customServiceRequestHeader) ??
                   new SafeDisposable())
            {
                try
                {
                    var result = InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger?.CallServiceFailed(ServiceUri, methodName, messageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }

        private string GetServiceMethodName(ServiceRemotingMessageHeaders messageHeaders)
        {
            if (messageHeaders == null)
                return null;

            return GetServiceMethodName(messageHeaders.InterfaceId, messageHeaders.MethodId);
        }

        private CustomServiceRequestHeader UpdateAndGetMessageHeaders(ServiceRemotingMessageHeaders messageHeaders)
        {
            var customHeader = ServiceRequestContext.Current?.GetCustomHeader();
            if (customHeader != null)
                if (!messageHeaders.HasHeader(customHeader.HeaderName))
                    messageHeaders.AddHeader(customHeader);

            return customHeader;
        }
    }
}