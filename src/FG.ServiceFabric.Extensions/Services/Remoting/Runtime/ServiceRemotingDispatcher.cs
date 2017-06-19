using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ServiceRemotingDispatcher : Microsoft.ServiceFabric.Services.Remoting.Runtime.ServiceRemotingDispatcher
    {
        private readonly ServiceContext _serviceContext;
        private readonly IService _service;
        private static readonly IDictionary<long, string> MethodMap = new ConcurrentDictionary<long, string>();        


        private string GetMethodName(int interfaceId, int methodId)
        {
            try
            {
                var lookup = HashUtil.Combine(interfaceId, methodId);
                if (MethodMap.ContainsKey(lookup))
                {
                    return MethodMap[lookup];
                }
                var methodName = this.GetMethodDispatcherMapName(interfaceId, methodId);
                MethodMap[lookup] = methodName;
                return methodName;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {

            }
            return "-";
        }

        public ServiceRemotingDispatcher(ServiceContext serviceContext, IService service) : base(serviceContext, service)
        {
            _serviceContext = serviceContext;
            _service = service;
        }

        protected virtual void TrackMethod(ServiceMethodInvocationInfo serviceMethodInvocationInfo)
        {
            /*
            var requestTelemetry = new RequestTelemetry(methodName,
                        startTime: start,
                        duration: DateTime.Now - start,
                        responseCode: "200",
                        success: true)
            { };
            requestTelemetry.Properties.Add("ServiceName", _serviceContext.ServiceName.ToString());
            requestTelemetry.Properties.Add("ServiceInterfaceId", messageHeaders.InterfaceId.ToString());
            requestTelemetry.Properties.Add("ReplicaOrInstanceId", _serviceContext.ReplicaOrInstanceId.ToString());
            requestTelemetry.Properties.Add("PartitionId", _serviceContext.PartitionId.ToString());
            requestTelemetry.Properties.Add("NodeName", _serviceContext.NodeContext.NodeName);
            requestTelemetry.Properties.Add("Method", methodName);
            requestTelemetry.Properties.Add("MethodId", messageHeaders.MethodId.ToString());
            requestTelemetry.Properties.Add("InvocationId", messageHeaders.InvocationId);

            _telemetryClient.TrackRequest(requestTelemetry);
            */
        }

        protected virtual void TrackException(ServiceMethodInvocationInfo serviceMethodInvocationInfo, Exception exception)
        {
            /*
            var properties = new Dictionary<string, string>();
            properties.Add("ServiceName", _serviceContext.ServiceName.ToString());
            properties.Add("ServiceInterfaceId", messageHeaders.InterfaceId.ToString());
            properties.Add("ReplicaOrInstanceId", _serviceContext.ReplicaOrInstanceId.ToString());
            properties.Add("PartitionId", _serviceContext.PartitionId.ToString());
            properties.Add("NodeName", _serviceContext.NodeContext.NodeName);
            properties.Add("Method", methodName);
            properties.Add("MethodId", messageHeaders.MethodId.ToString());
            properties.Add("InvocationId", messageHeaders.InvocationId);

            _telemetryClient.TrackException(ex, properties);
            */
        }

        public override void HandleOneWay(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            base.HandleOneWay(requestContext, messageHeaders, requestBody);
        }

        public override async Task<byte[]> RequestResponseAsync(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBodyBytes)
        {
            var methodName = GetMethodName(messageHeaders.InterfaceId, messageHeaders.MethodId);

            byte[] result = null;
            var start = DateTime.Now;
            try
            {
                var headers = messageHeaders.GetHeaders(_serviceContext, _service);

                result = await this.RunInRequestContext(
                    async () => await base.RequestResponseAsync(
                        requestContext,
                        messageHeaders,
                        requestBodyBytes),
                    headers);

                TrackMethod(new ServiceMethodInvocationInfo(_serviceContext, messageHeaders, methodName, messageHeaders.InvocationId, start, DateTime.Now - start, true));
            }
            catch (Exception ex)
            {
                TrackException(new ServiceMethodInvocationInfo(_serviceContext, messageHeaders, methodName, messageHeaders.InvocationId, start), ex);
                throw;
            }
            return result;
        }
    }
}