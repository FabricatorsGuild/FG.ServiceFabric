using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport.Client
{
    public class FabricTransportServiceRemotingClient : IServiceRemotingClient, ICommunicationClient
    {
        public IEnumerable<ServiceRequestHeader> Headers { get; private set; }
        private readonly IServiceRemotingClient _innerClient;

        public FabricTransportServiceRemotingClient(IServiceRemotingClient innerClient, IEnumerable<ServiceRequestHeader> headers)
        {
            Headers = headers;
            _innerClient = innerClient;
        }

        ~FabricTransportServiceRemotingClient()
        {
            if (this._innerClient == null) return;
            // ReSharper disable once SuspiciousTypeConversion.Global
            var disposable = this._innerClient as IDisposable;
            disposable?.Dispose();
        }        

        Task<byte[]> IServiceRemotingClient.RequestResponseAsync(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            messageHeaders.AddHeaders(Headers);
            return this._innerClient.RequestResponseAsync(messageHeaders, requestBody);
        }

        void IServiceRemotingClient.SendOneWay(ServiceRemotingMessageHeaders messageHeaders, byte[] requestBody)
        {
            messageHeaders.AddHeaders(Headers);
            this._innerClient.SendOneWay(messageHeaders, requestBody);
        }

        public ResolvedServicePartition ResolvedServicePartition
        {
            get { return this._innerClient.ResolvedServicePartition; }
            set { this._innerClient.ResolvedServicePartition = value; }
        }

        public string ListenerName
        {
            get { return this._innerClient.ListenerName; }
            set { this._innerClient.ListenerName = value; }
        }
        public ResolvedServiceEndpoint Endpoint
        {
            get { return this._innerClient.Endpoint; }
            set { this._innerClient.Endpoint = value; }
        }
    }
}