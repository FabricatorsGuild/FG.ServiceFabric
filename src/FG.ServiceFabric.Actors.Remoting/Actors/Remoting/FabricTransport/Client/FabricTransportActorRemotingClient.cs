using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Remoting.Runtime;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using FG.ServiceFabric.Services.Remoting.FabricTransport.Client;
using FG.ServiceFabric.Utils;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Builder;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Actors.Remoting.FabricTransport.Client
{
    public class FabricTransportActorRemotingClient : FabricTransportServiceRemotingClient
    {
	    internal IServiceRemotingClient InnerClient
	    {
		    get { return base.InnerClient; }
	    }

	    private readonly IActorClientLogger _logger;
        private readonly MethodDispatcherBase _actorMethodDispatcher;
        private static readonly ConcurrentDictionary<long, string> ActorMethodMap = new ConcurrentDictionary<long, string>();

        private string GetActorMethodName(ActorMessageHeaders actorMessageHeaders)
        {
            if (actorMessageHeaders == null) return null;
            try
            {
                var methodName = "-";
                var lookup = HashUtil.Combine(actorMessageHeaders.InterfaceId, actorMessageHeaders.MethodId);

                if (ActorMethodMap.ContainsKey(lookup))
                {
                    methodName = ActorMethodMap[lookup];
                    return methodName;
                }

                methodName = _actorMethodDispatcher.GetMethodDispatcherMapName(
                    actorMessageHeaders.InterfaceId, actorMessageHeaders.MethodId);
                ActorMethodMap[lookup] = methodName;
                return methodName;
            }
            catch (Exception ex)
            {
                
                // ignored
                //_logger?.FailedToGetActorMethodName(actorMessageHeaders, ex);
            }
            return null;
        }
        
        public FabricTransportActorRemotingClient(IServiceRemotingClient innerClient, Uri serviceUri, IActorClientLogger logger, 
            MethodDispatcherBase actorMethodDispatcher, MethodDispatcherBase serviceMethodDispatcher)
            : base(innerClient, serviceUri, logger, serviceMethodDispatcher)
        {
            _logger = logger;
            _actorMethodDispatcher = actorMethodDispatcher;
        }

        ~FabricTransportActorRemotingClient()
        {
            if (this.InnerClient == null) return;
            // ReSharper disable once SuspiciousTypeConversion.Global
            var disposable = this.InnerClient as IDisposable;
            disposable?.Dispose();
        }

        protected override Task<byte[]> RequestServiceResponseAsync(ServiceRemotingMessageHeaders messageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var actorMessageHeaders = GetActorMessageHeaders(messageHeaders);
            if (actorMessageHeaders != null)
            {
                return RequestActorResponseAsync(messageHeaders, actorMessageHeaders, customServiceRequestHeader, requestBody);
            }
            return base.RequestServiceResponseAsync(messageHeaders, customServiceRequestHeader, requestBody);
        }        

        private Task<byte[]> RequestActorResponseAsync(ServiceRemotingMessageHeaders messageHeaders, ActorMessageHeaders actorMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var methodName = GetActorMethodName(actorMessageHeaders);
            using (_logger.CallActor(ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader))
            {
                try
                {
                    var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.CallActorFailed(ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }

        protected override Task<byte[]> SendServiceOneWay(ServiceRemotingMessageHeaders messageHeaders, CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var actorMessageHeaders = GetActorMessageHeaders(messageHeaders);
            if (actorMessageHeaders != null)
            {
                return SendActorOneWay(messageHeaders, actorMessageHeaders, customServiceRequestHeader, requestBody);
            }
            return base.SendServiceOneWay(messageHeaders, customServiceRequestHeader, requestBody);
        }
        
        private Task<byte[]> SendActorOneWay(ServiceRemotingMessageHeaders messageHeaders, ActorMessageHeaders actorMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var methodName = GetActorMethodName(actorMessageHeaders);
            using (_logger.CallActor(ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader))
            {
                try
                {
                    var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.CallActorFailed(ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }

        private static ActorMessageHeaders GetActorMessageHeaders(ServiceRemotingMessageHeaders messageHeaders)
        {
            ActorMessageHeaders actorMessageHeaders = null;
            if (ActorMessageHeaders.TryFromServiceMessageHeaders(messageHeaders, out actorMessageHeaders))
            {
                
            }
            return actorMessageHeaders;
        }
    }
}