using System;
using System.Collections.Concurrent;
using System.Fabric;
using System.Threading.Tasks;

using FG.Common.Utils;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.Runtime;

using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Builder;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport.Client
{
    public class FabricTransportServiceRemotingClient : IServiceRemotingClient, ICommunicationClient
    {
        private static readonly ConcurrentDictionary<long, string> ServiceMethodMap = new ConcurrentDictionary<long, string>()
            ;

        private readonly IServiceClientLogger _logger;

        private readonly MethodDispatcherBase[] _serviceMethodDispatchers;
        protected readonly Uri ServiceUri;

        public FabricTransportServiceRemotingClient(IServiceRemotingClient innerClient, Uri serviceUri,
            IServiceClientLogger logger,
            MethodDispatcherBase[] serviceMethodDispatchers)
        {
            this.InnerClient = innerClient;
            this.ServiceUri = serviceUri;
            this._logger = logger;
            this._serviceMethodDispatchers = serviceMethodDispatchers;
        }

        protected internal IServiceRemotingClient InnerClient { get; }

        Task<byte[]> IServiceRemotingClient.RequestResponseAsync(ServiceRemotingMessageHeaders messageHeaders,
            byte[] requestBody)
        {
            var customServiceRequestHeader = this.UpdateAndGetMessageHeaders(messageHeaders);
            return this.RequestServiceResponseAsync(messageHeaders, customServiceRequestHeader, requestBody);
        }

        void IServiceRemotingClient.SendOneWay(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            var customServiceRequestHeader = this.UpdateAndGetMessageHeaders(messageHeaders);

            this.SendServiceOneWay(messageHeaders, customServiceRequestHeader, requestBody);
        }

        public ResolvedServicePartition ResolvedServicePartition
        {
            get { return this.InnerClient.ResolvedServicePartition; }
            set { this.InnerClient.ResolvedServicePartition = value; }
        }

        public string ListenerName
        {
            get { return this.InnerClient.ListenerName; }
            set { this.InnerClient.ListenerName = value; }
        }

        public ResolvedServiceEndpoint Endpoint
        {
            get { return this.InnerClient.Endpoint; }
            set { this.InnerClient.Endpoint = value; }
        }

        private string GetServiceMethodName(ServiceRemotingMessageHeaders messageHeaders)
        {
            if (messageHeaders == null) return null;

            return this.GetServiceMethodName(messageHeaders.InterfaceId, messageHeaders.MethodId);
        }

        protected string GetServiceMethodName(int interfaceId, int methodId)
        {
            try
            {
                var methodName = "-";
                var lookup = HashUtil.Combine(interfaceId, methodId);

                return ServiceMethodMap.GetOrAdd(
                    lookup,
                    lu =>
                        {
                            foreach (var serviceMethodDispatcher in this._serviceMethodDispatchers)
                            {
                                methodName = serviceMethodDispatcher.GetMethodDispatcherMapName(interfaceId, methodId);
                                if (methodName != null)
                                {
                                    return methodName;
                                }
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

        ~FabricTransportServiceRemotingClient()
        {
            if (this.InnerClient == null) return;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var disposable = this.InnerClient as IDisposable;
            disposable?.Dispose();
        }

        protected virtual Task<byte[]> RequestServiceResponseAsync(ServiceRemotingMessageHeaders messageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var methodName = this.GetServiceMethodName(messageHeaders);
            using (this._logger?.CallService(this.ServiceUri, methodName, messageHeaders, customServiceRequestHeader) ??
                   new SafeDisposable())
            {
                try
                {
                    var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    this._logger?.CallServiceFailed(this.ServiceUri, methodName, messageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }

        protected virtual Task<byte[]> SendServiceOneWay(ServiceRemotingMessageHeaders messageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var methodName = this.GetServiceMethodName(messageHeaders);
            using (this._logger?.CallService(this.ServiceUri, methodName, messageHeaders, customServiceRequestHeader) ??
                   new SafeDisposable())
            {
                try
                {
                    var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    this._logger?.CallServiceFailed(this.ServiceUri, methodName, messageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }

        private CustomServiceRequestHeader UpdateAndGetMessageHeaders(ServiceRemotingMessageHeaders messageHeaders)
        {
            var customHeader = ServiceRequestContext.Current?.GetCustomHeader();
            if (customHeader != null)
            {
                if (!messageHeaders.HasHeader(customHeader.HeaderName))
                {
                    messageHeaders.AddHeader(customHeader);
                }
            }

            return customHeader;
        }
    }
}