namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using FG.Common.Utils;
    using FG.ServiceFabric.Diagnostics;
    using FG.ServiceFabric.Services.Remoting.FabricTransport;
    using FG.ServiceFabric.Services.Runtime;

    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.V1;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Runtime;

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ServiceRemotingDispatcher : IServiceRemotingMessageHandler
    {
        private static readonly ConcurrentDictionary<long, string> ServiceMethodMap = new ConcurrentDictionary<long, string>();

        private readonly IServiceRemotingMessageHandler _innerMessageHandler;

        private readonly IServiceCommunicationLogger _logger;

        private readonly IService _service;

        public ServiceRemotingDispatcher(IService service, IServiceRemotingMessageHandler innerMessageHandler, IServiceCommunicationLogger logger)
        {
            this._service = service;
            this._innerMessageHandler = innerMessageHandler;
            this._logger = logger;
        }

        public void HandleOneWay(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> RequestResponseAsync(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            var customHeader = messageHeaders.GetCustomServiceRequestHeader(this._logger) ?? new CustomServiceRequestHeader();
            return this.RequestResponseServiceMessageAsync(requestContext, messageHeaders, requestBody, customHeader);
        }

        private string GetServiceMethodName(int interfaceId, int methodId)
        {
            try
            {
                var lookup = HashUtil.Combine(interfaceId, methodId);

                return ServiceMethodMap.GetOrAdd(
                    lookup,
                    lu => ((Microsoft.ServiceFabric.Services.Remoting.V1.Runtime.ServiceRemotingDispatcher)this._innerMessageHandler).GetMethodDispatcherMapName(
                        interfaceId,
                        methodId));
            }
            catch (Exception ex)
            {
                // Ignored
                this._logger?.FailedToGetServiceMethodName(this._service.GetServiceContext().ServiceName, interfaceId, methodId, ex);
            }

            return null;
        }

        private async Task<byte[]> RequestResponseServiceMessageAsync(
            IServiceRemotingRequestContext requestContext,
            ServiceRemotingMessageHeaders messageHeaders,
            byte[] requestBody,
            CustomServiceRequestHeader customHeader)
        {
            var methodName = this.GetServiceMethodName(messageHeaders.InterfaceId, messageHeaders.MethodId);

            byte[] result = null;
            using (new ServiceRequestContextWrapper(customHeader))
            {
                using (this._logger?.RecieveServiceMessage(this._service.GetServiceContext().ServiceName, methodName, messageHeaders, customHeader))
                {
                    try
                    {
                        result = await this._innerMessageHandler.RequestResponseAsync(requestContext, messageHeaders, requestBody);
                    }
                    catch (Exception ex)
                    {
                        this._logger?.RecieveServiceMessageFailed(this._service.GetServiceContext().ServiceName, methodName, messageHeaders, customHeader, ex);
                        throw;
                    }
                }
            }

            return result;
        }
    }
}