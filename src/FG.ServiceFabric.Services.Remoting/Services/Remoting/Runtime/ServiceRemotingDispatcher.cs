using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Utils;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ServiceRemotingDispatcher : IServiceRemotingMessageHandler
    {
        private static readonly IDictionary<long, string> ServiceMethodMap = new ConcurrentDictionary<long, string>();

        private readonly IService _service;
        private readonly IServiceRemotingMessageHandler _innerMessageHandler;
        private readonly IServiceCommunicationLogger _logger;
        
        private string GetServiceMethodName(int interfaceId, int methodId)
        {
            try
            {
                var lookup = HashUtil.Combine(interfaceId, methodId);
                if (ServiceMethodMap.ContainsKey(lookup))
                {
                    return ServiceMethodMap[lookup];
                }
                var methodName = ((Microsoft.ServiceFabric.Services.Remoting.Runtime.ServiceRemotingDispatcher)_innerMessageHandler).GetMethodDispatcherMapName(interfaceId, methodId);
                ServiceMethodMap[lookup] = methodName;
                return methodName;
            }
            catch (Exception ex)
            {
                // Ignored
                _logger?.FailedToGetServiceMethodName(_service.GetServiceContext().ServiceName, interfaceId, methodId, ex);
            }
            return null;
        }

        public ServiceRemotingDispatcher(IService service, IServiceRemotingMessageHandler innerMessageHandler, IServiceCommunicationLogger logger)
        {
            _service = service;
            _innerMessageHandler = innerMessageHandler;
            _logger = logger;
        }

        public Task<byte[]> RequestResponseAsync(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            var customHeader = messageHeaders.GetCustomServiceRequestHeader(_logger) ?? new CustomServiceRequestHeader();
            return RequestResponseServiceMessageAsync(requestContext, messageHeaders, requestBody, customHeader);
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
                using (_logger?.RecieveServiceMessage(_service.GetServiceContext().ServiceName, methodName, messageHeaders, customHeader))
                {
                    try
                    {
                        result = await _innerMessageHandler.RequestResponseAsync(requestContext, messageHeaders, requestBody);
                    }
                    catch (Exception ex)
                    {
                        _logger?.RecieveServiceMessageFailed(_service.GetServiceContext().ServiceName, methodName, messageHeaders, customHeader, ex);
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