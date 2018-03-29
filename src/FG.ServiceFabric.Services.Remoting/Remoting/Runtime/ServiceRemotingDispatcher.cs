using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using FG.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Runtime;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    /// <summary>
    ///     Provides dispathching services for service remoting
    /// </summary>
    public class ServiceRemotingDispatcher : IServiceRemotingMessageHandler
    {
        private static readonly ConcurrentDictionary<long, string> ServiceMethodMap =
            new ConcurrentDictionary<long, string>();

        private readonly IServiceRemotingMessageHandler _innerMessageHandler;

        private readonly IServiceCommunicationLogger _logger;

        private readonly IService _service;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceRemotingDispatcher" /> class.
        /// </summary>
        /// <param name="service">
        ///     The service interface
        /// </param>
        /// <param name="innerMessageHandler">
        ///     The inner message handler
        /// </param>
        /// <param name="logger">
        ///     A service communication logger
        /// </param>
        public ServiceRemotingDispatcher(IService service, IServiceRemotingMessageHandler innerMessageHandler,
            IServiceCommunicationLogger logger)
        {
            _service = service;
            _innerMessageHandler = innerMessageHandler;
            _logger = logger;
        }

        public void HandleOneWay(IServiceRemotingRequestContext requestContext,
            ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> RequestResponseAsync(IServiceRemotingRequestContext requestContext,
            ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            var customHeader = messageHeaders.GetCustomServiceRequestHeader(_logger) ??
                               new CustomServiceRequestHeader();
            return RequestResponseServiceMessageAsync(requestContext, messageHeaders, requestBody, customHeader);
        }

        private string GetServiceMethodName(int interfaceId, int methodId)
        {
            try
            {
                var lookup = HashUtil.Combine(interfaceId, methodId);

                return ServiceMethodMap.GetOrAdd(
                    lookup,
                    lu =>
                        ((Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher)
                            _innerMessageHandler).GetMethodDispatcherMapName(
                            interfaceId,
                            methodId));
            }
            catch (Exception ex)
            {
                // Ignored
                _logger?.FailedToGetServiceMethodName(_service.GetServiceContext().ServiceName, interfaceId, methodId,
                    ex);
            }

            return null;
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
                using (_logger?.RecieveServiceMessage(_service.GetServiceContext().ServiceName, methodName,
                    messageHeaders, customHeader))
                {
                    try
                    {
                        result = await _innerMessageHandler.RequestResponseAsync(requestContext, messageHeaders,
                            requestBody);
                    }
                    catch (Exception ex)
                    {
                        _logger?.RecieveServiceMessageFailed(_service.GetServiceContext().ServiceName, methodName,
                            messageHeaders, customHeader, ex);
                        throw;
                    }
                }
            }

            return result;
        }
    }
}