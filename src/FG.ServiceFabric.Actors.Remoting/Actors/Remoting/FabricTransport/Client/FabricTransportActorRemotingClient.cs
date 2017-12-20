namespace FG.ServiceFabric.Actors.Remoting.FabricTransport.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using FG.Common.Utils;
    using FG.ServiceFabric.Actors.Remoting.Runtime;
    using FG.ServiceFabric.Diagnostics;
    using FG.ServiceFabric.Services.Remoting.FabricTransport;
    using FG.ServiceFabric.Services.Remoting.FabricTransport.Client;

    using Microsoft.ServiceFabric.Services.Remoting.Builder;
    using Microsoft.ServiceFabric.Services.Remoting.V1;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

    public class FabricTransportActorRemotingClient : FabricTransportServiceRemotingClient
    {
        private static readonly ConcurrentDictionary<long, string> ActorMethodMap = new ConcurrentDictionary<long, string>();

        private readonly IActorClientLogger _logger;

        public FabricTransportActorRemotingClient(IServiceRemotingClient innerClient, Uri serviceUri, IActorClientLogger logger, MethodDispatcherBase[] serviceMethodDispatchers)
            : base(innerClient, serviceUri, logger, serviceMethodDispatchers)
        {
            this._logger = logger;
        }

        ~FabricTransportActorRemotingClient()
        {
            if (this.InnerClient == null)
            {
                return;
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            var disposable = this.InnerClient as IDisposable;
            disposable?.Dispose();
        }

        internal new IServiceRemotingClient InnerClient => base.InnerClient;

        protected override Task<byte[]> RequestServiceResponseAsync(
            ServiceRemotingMessageHeaders messageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader,
            byte[] requestBody)
        {
            var actorMessageHeaders = GetActorMessageHeaders(messageHeaders);
            if (actorMessageHeaders != null)
            {
                return this.RequestActorResponseAsync(messageHeaders, actorMessageHeaders, customServiceRequestHeader, requestBody);
            }

            return base.RequestServiceResponseAsync(messageHeaders, customServiceRequestHeader, requestBody);
        }

        protected override Task<byte[]> SendServiceOneWay(ServiceRemotingMessageHeaders messageHeaders, CustomServiceRequestHeader customServiceRequestHeader, byte[] requestBody)
        {
            var actorMessageHeaders = GetActorMessageHeaders(messageHeaders);
            if (actorMessageHeaders != null)
            {
                return this.SendActorOneWay(messageHeaders, actorMessageHeaders, customServiceRequestHeader, requestBody);
            }

            return base.SendServiceOneWay(messageHeaders, customServiceRequestHeader, requestBody);
        }

        private static ActorMessageHeaders GetActorMessageHeaders(ServiceRemotingMessageHeaders messageHeaders)
        {
            ActorMessageHeaders actorMessageHeaders = null;
            if (ActorMessageHeaders.TryFromServiceMessageHeaders(messageHeaders, out actorMessageHeaders))
            {
            }

            return actorMessageHeaders;
        }

        private string GetActorMethodName(ActorMessageHeaders actorMessageHeaders)
        {
            if (actorMessageHeaders == null)
            {
                return null;
            }

            return this.GetServiceMethodName(actorMessageHeaders.InterfaceId, actorMessageHeaders.MethodId);
        }

        private Task<byte[]> RequestActorResponseAsync(
            ServiceRemotingMessageHeaders messageHeaders,
            ActorMessageHeaders actorMessageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader,
            byte[] requestBody)
        {
            var methodName = this.GetActorMethodName(actorMessageHeaders);
            using (this._logger?.CallActor(this.ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader) ?? new SafeDisposable())
            {
                try
                {
                    var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    this._logger?.CallActorFailed(this.ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }

        private Task<byte[]> SendActorOneWay(
            ServiceRemotingMessageHeaders messageHeaders,
            ActorMessageHeaders actorMessageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader,
            byte[] requestBody)
        {
            var methodName = this.GetActorMethodName(actorMessageHeaders);
            using (this._logger?.CallActor(this.ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader) ?? new SafeDisposable())
            {
                try
                {
                    var result = this.InnerClient.RequestResponseAsync(messageHeaders, requestBody);
                    return result;
                }
                catch (Exception ex)
                {
                    this._logger?.CallActorFailed(this.ServiceUri, methodName, actorMessageHeaders, customServiceRequestHeader, ex);
                    throw;
                }
            }
        }
    }
}