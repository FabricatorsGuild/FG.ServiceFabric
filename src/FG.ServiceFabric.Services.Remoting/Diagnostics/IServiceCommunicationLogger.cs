using System;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Diagnostics
{
    public interface IServiceCommunicationLogger : IServiceRemotingLogger
    {
        IDisposable RecieveServiceMessage(Uri requestUri, string serviceMethodName, ServiceRemotingMessageHeaders serviceMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader);

        void RecieveServiceMessageFailed(Uri requestUri, string serviceMethodName, ServiceRemotingMessageHeaders serviceMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader,
            Exception ex);

        void FailedToGetServiceMethodName(Uri requestUri, int interfaceId, int methodId, Exception ex);
    }
}