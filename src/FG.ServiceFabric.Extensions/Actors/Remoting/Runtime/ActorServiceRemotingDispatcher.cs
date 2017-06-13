using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using FG.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace FG.ServiceFabric.Actors.Remoting.Runtime
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ActorServiceRemotingDispatcher : Microsoft.ServiceFabric.Actors.Remoting.Runtime.ActorServiceRemotingDispatcher
    {
        private readonly ActorService _actorService;
        private static readonly ConcurrentDictionary<long, string> MethodMap = new ConcurrentDictionary<long, string>();

        private string GetActorMethodName(ActorMessageHeaders actorMessageHeaders)
        {
            if (actorMessageHeaders == null) return null;
            try
            {
                var methodName = "-";
                var lookup = HashUtil.Combine(actorMessageHeaders.InterfaceId, actorMessageHeaders.MethodId);
                if (MethodMap.ContainsKey(lookup))
                {
                    methodName = MethodMap[lookup];
                    return methodName;
                }

                methodName = ((Microsoft.ServiceFabric.Actors.Remoting.Runtime.ActorServiceRemotingDispatcher)this).GetMethodDispatcherMapName(
                    actorMessageHeaders.InterfaceId, actorMessageHeaders.MethodId);
                MethodMap[lookup] = methodName;
                return methodName;
            }
            catch (Exception)
            {
                // ignored
            }
            return null;
        }

        protected virtual void TrackMethod(ActorServiceMethodInvocationInfo actorServiceMethodInvocationInfo)
        {
            /*
                var requestTelemetry = new RequestTelemetry(actorMethodInformation.MethodName,
                        startTime: start,
                        duration: DateTime.Now - start,
                        responseCode: "200",
                        success: true)
                    { };
                requestTelemetry.Properties.Add("ServiceName", _actorService.Context.ServiceName.ToString());
                requestTelemetry.Properties.Add("ServiceInterfaceId", messageHeaders.InterfaceId.ToString());
                requestTelemetry.Properties.Add("ReplicaOrInstanceId", _actorService.Context.ReplicaOrInstanceId.ToString());
                requestTelemetry.Properties.Add("PartitionId", _actorService.Context.PartitionId.ToString());
                requestTelemetry.Properties.Add("NodeName", _actorService.Context.NodeContext.NodeName);
                requestTelemetry.Properties.Add("Actor", _actorService.ActorTypeInformation.ServiceName);
                requestTelemetry.Properties.Add("ActorId", actorMethodInformation.ActorId.ToString());
                requestTelemetry.Properties.Add("Method", actorMethodInformation.MethodName);
                requestTelemetry.Properties.Add("MethodId", messageHeaders.MethodId.ToString());
                requestTelemetry.Properties.Add("InvocationId", messageHeaders.InvocationId);

                _telemetryClient.TrackRequest(requestTelemetry);
            */
        }

        protected virtual void TrackException(ActorServiceMethodInvocationInfo actorServiceMethodInvocationInfo, Exception exception)
        {
            /*
                var properties = new Dictionary<string, string>();
                properties.Add("ServiceName", _actorService.Context.ServiceName.ToString());
                properties.Add("ServiceInterfaceId", messageHeaders.InterfaceId.ToString());
                properties.Add("ReplicaOrInstanceId", _actorService.Context.ReplicaOrInstanceId.ToString());
                properties.Add("PartitionId", _actorService.Context.PartitionId.ToString());
                properties.Add("NodeName", _actorService.Context.NodeContext.NodeName);
                properties.Add("Actor", _actorService.ActorTypeInformation.ServiceName);
                properties.Add("ActorId", actorMethodInformation.ActorId.ToString());
                properties.Add("Method", actorMethodInformation.MethodName);
                properties.Add("MethodId", messageHeaders.MethodId.ToString());
                properties.Add("InvocationId", messageHeaders.InvocationId);

                _telemetryClient.TrackException(ex, properties);
            */
        }

        public ActorServiceRemotingDispatcher(ActorService actorService) : base(actorService)
        {
            _actorService = actorService;
        }

        public override async Task<byte[]> RequestResponseAsync(IServiceRemotingRequestContext requestContext, ServiceRemotingMessageHeaders messageHeaders, byte[] requestBodyBytes)
        {
            var actorMessageHeaders = messageHeaders.GetActorMessageHeaders();
            var methodName = GetActorMethodName(actorMessageHeaders);
            var start = DateTime.Now;
            byte[] result = null;
            try
            {
                var headers = messageHeaders.GetHeaders(_actorService.Context, _actorService);

                result = await this.RunInRequestContext(
                    async () => await base.RequestResponseAsync(
                        requestContext,
                        messageHeaders,
                        requestBodyBytes),
                    headers);
                 
                TrackMethod(new ActorServiceMethodInvocationInfo(
                    _actorService.Context, 
                    messageHeaders,
                    methodName, 
                    messageHeaders.InvocationId,
                    actorMessageHeaders?.ActorId, 
                    _actorService.ActorTypeInformation.ServiceName, 
                    start, 
                    DateTime.Now - start, 
                    true));
            }
            catch (Exception ex)
            {
                TrackException(new ActorServiceMethodInvocationInfo(
                    _actorService.Context,
                    messageHeaders,
                    methodName,
                    messageHeaders.InvocationId,
                    actorMessageHeaders?.ActorId,
                    _actorService.ActorTypeInformation.ServiceName,
                    start), ex);
                throw;
            }
            return result;
        }
    }
}