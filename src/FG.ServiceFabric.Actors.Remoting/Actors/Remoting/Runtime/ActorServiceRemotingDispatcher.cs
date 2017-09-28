using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using FG.ServiceFabric.Services.Remoting.Runtime;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Runtime;

namespace FG.ServiceFabric.Actors.Remoting.Runtime
{

    public class ActorServiceRemotingDispatcher : IServiceRemotingMessageHandler
    {
        private static readonly IDictionary<long, string> ServiceMethodMap = new ConcurrentDictionary<long, string>();
        private static readonly ConcurrentDictionary<long, string> ActorMethodMap = new ConcurrentDictionary<long, string>();

        private readonly ActorService _actorService;
        private readonly IServiceRemotingMessageHandler _innerMessageHandler;
        private readonly IActorServiceCommunicationLogger _logger;

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

                methodName = ((Microsoft.ServiceFabric.Actors.Remoting.V1.Runtime.ActorServiceRemotingDispatcher)_innerMessageHandler).GetMethodDispatcherMapName(
                    actorMessageHeaders.InterfaceId, actorMessageHeaders.MethodId);
                ActorMethodMap[lookup] = methodName;
                return methodName;
            }
            catch (Exception ex)
            {
                // ignored
                _logger?.FailedToGetActorMethodName(actorMessageHeaders, ex);
            }
            return null;
        }

        private string GetServiceMethodName(int interfaceId, int methodId)
        {
            try
            {
                var lookup = HashUtil.Combine(interfaceId, methodId);
                if (ServiceMethodMap.ContainsKey(lookup))
                {
                    return ServiceMethodMap[lookup];
                }
                var methodName = ((Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher)_innerMessageHandler).GetMethodDispatcherMapName(interfaceId, methodId);
                ServiceMethodMap[lookup] = methodName;
                return methodName;
            }
            catch (Exception ex)
            {
                // Ignored
                _logger?.FailedToGetServiceMethodName(_actorService.Context.ServiceName, interfaceId, methodId, ex);
            }
            return null;
        }

        public ActorServiceRemotingDispatcher(ActorService actorService, IServiceRemotingMessageHandler innerMessageHandler, IActorServiceCommunicationLogger logger)
        {
            _actorService = actorService;
            _innerMessageHandler = innerMessageHandler;
            _logger = logger;
        }

        public Task<byte[]> RequestResponseAsync(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            var customHeader = messageHeaders.GetCustomServiceRequestHeader(_logger) ?? new CustomServiceRequestHeader();
            var actorMessageHeaders = messageHeaders.GetActorMessageHeaders(_logger);
            if (actorMessageHeaders == null)
            {
                return RequestResponseServiceMessageAsync(requestContext, messageHeaders, requestBody, customHeader);
            }
            else
            {
                return RequestResponseActorMessageAsync(requestContext, messageHeaders, requestBody, actorMessageHeaders, customHeader);
            }
        }

        private async Task<byte[]> RequestResponseActorMessageAsync(
            IServiceRemotingRequestContext requestContext,
            ServiceRemotingMessageHeaders messageHeaders,
            byte[] requestBody,
            ActorMessageHeaders actorMessageHeaders,
            CustomServiceRequestHeader customHeader)
        {
            var methodName = GetActorMethodName(actorMessageHeaders);

            byte[] result = null;
            using (new ServiceRequestContextWrapper(customHeader))
            {
                using (_logger?.RecieveActorMessage(_actorService.Context.ServiceName, methodName, actorMessageHeaders, customHeader))
                {
                    try
                    {
                        result = await _innerMessageHandler.RequestResponseAsync(requestContext, messageHeaders, requestBody);
                    }
                    catch (Exception ex)
                    {
                        _logger?.RecieveActorMessageFailed(_actorService.Context.ServiceName, methodName, actorMessageHeaders, customHeader, ex);
                        throw;
                    }
                }
            }
            return result;
        }


        private async Task<byte[]> RequestResponseServiceMessageAsync(
            IServiceRemotingRequestContext requestContext, 
            ServiceRemotingMessageHeaders messageHeaders,
            byte[] requestBody,
            CustomServiceRequestHeader customHeader)
        {
            var methodName = GetServiceMethodName(messageHeaders.InterfaceId, messageHeaders.MethodId);


            byte[] result = null;
            using (new ServiceRequestContextWrapper(customHeader))
            {
                using (_logger?.RecieveServiceMessage(_actorService.Context.ServiceName, methodName, messageHeaders, customHeader))
                {
                    try
                    {
                        result = await _innerMessageHandler.RequestResponseAsync(requestContext, messageHeaders, requestBody);
                    }
                    catch (Exception ex)
                    {
                        _logger?.RecieveServiceMessageFailed(_actorService.Context.ServiceName, methodName, messageHeaders, customHeader, ex);
                        throw;
                    }
                }
            }
            return result;
        }

        public void HandleOneWay(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            throw new NotImplementedException();
        }
    }
}