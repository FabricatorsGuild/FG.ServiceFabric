using System;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.V1;

namespace FG.ServiceFabric.Diagnostics
{
    /// <summary>
    ///     Provides logging for server side service communication
    /// </summary>
    public interface IServiceCommunicationLogger : IServiceRemotingLogger
    {
        void FailedToGetServiceMethodName(Uri requestUri, int interfaceId, int methodId, Exception ex);

        IDisposable RecieveServiceMessage(
            Uri requestUri,
            string serviceMethodName,
            ServiceRemotingMessageHeaders serviceMessageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader);

        void RecieveServiceMessageFailed(
            Uri requestUri,
            string serviceMethodName,
            ServiceRemotingMessageHeaders serviceMessageHeaders,
            CustomServiceRequestHeader customServiceRequestHeader,
            Exception ex);
    }
}