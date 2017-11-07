using System;
using System.Collections.Concurrent;
using System.Fabric;
using System.Threading.Tasks;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.Runtime;
using FG.Common.Utils;
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
        protected internal IServiceRemotingClient InnerClient { get; }
        protected readonly Uri ServiceUri;
	    private readonly IServiceClientLogger _logger;

        private readonly MethodDispatcherBase[] _serviceMethodDispatchers;
        private static readonly ConcurrentDictionary<long, string> ServiceMethodMap = new ConcurrentDictionary<long, string>();

	    private string GetServiceMethodName(ServiceRemotingMessageHeaders messageHeaders)
	    {
		    if (messageHeaders == null) return null;

		    return GetServiceMethodName(messageHeaders.InterfaceId, messageHeaders.MethodId);
	    }

	    protected string GetServiceMethodName(int interfaceId, int methodId)
        {
            try
            {
                var methodName = "-";
                var lookup = HashUtil.Combine(interfaceId, methodId);
                if (ServiceMethodMap.ContainsKey(lookup))
                {
                    methodName = ServiceMethodMap[lookup];
                    return methodName;
                }

	            foreach(var serviceMethodDispatcher in _serviceMethodDispatchers)
	            {
					methodName = serviceMethodDispatcher.GetMethodDispatcherMapName(interfaceId, methodId);
		            if (methodName == null) continue;
		            ServiceMethodMap[lookup] = methodName;
		            return methodName;
	            }				
            }
            catch (Exception)
            {
                // ignored
                //_logger?.FailedToGetActorMethodName(actorMessageHeaders, ex);
            }
            return null;
        }

        public FabricTransportServiceRemotingClient(IServiceRemotingClient innerClient, Uri serviceUri, IServiceClientLogger logger,
            MethodDispatcherBase[] serviceMethodDispatchers)
        {
            InnerClient = innerClient;
            ServiceUri = serviceUri;
            _logger = logger;
            _serviceMethodDispatchers = serviceMethodDispatchers;
        }

        ~FabricTransportServiceRemotingClient()
        {
            if (this.InnerClient == null) return;
            // ReSharper disable once SuspiciousTypeConversion.Global
            var disposable = this.InnerClient as IDisposable;
            disposable?.Dispose();
        }       

        Task<byte[]> IServiceRemotingClient.RequestResponseAsync(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            var customServiceRequestHeader = UpdateAndGetMessageHeaders(messageHeaders);
            return RequestServiceResponseAsync(messageHeaders, customServiceRequestHeader, requestBody);
        }

        protected virtual Task<byte[]> RequestServiceResponseAsync(ServiceRemotingMessageHeaders messageHeaders, CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var methodName = GetServiceMethodName(messageHeaders);
            using (_logger?.CallService(ServiceUri, methodName, messageHeaders, customServiceRequestHeader) ?? new SafeDisposable())
            {
                try
                {
                    var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger?.CallServiceFailed(ServiceUri, methodName, messageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }

        void IServiceRemotingClient.SendOneWay(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            var customServiceRequestHeader = UpdateAndGetMessageHeaders(messageHeaders);

            SendServiceOneWay(messageHeaders, customServiceRequestHeader, requestBody);
        }

        protected virtual Task<byte[]> SendServiceOneWay(ServiceRemotingMessageHeaders messageHeaders, CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var methodName = GetServiceMethodName(messageHeaders);
            using (_logger?.CallService(ServiceUri, methodName, messageHeaders, customServiceRequestHeader) ?? new SafeDisposable())
            {
                try
                {
                    var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger?.CallServiceFailed(ServiceUri, methodName, messageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
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