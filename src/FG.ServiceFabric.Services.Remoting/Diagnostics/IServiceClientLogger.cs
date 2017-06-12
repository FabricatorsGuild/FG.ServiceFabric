using System;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Diagnostics
{
    public interface IServiceClientLogger : IServiceRemotingLogger
    {
        IDisposable CallService(Uri requestUri, string serviceMethodName, ServiceRemotingMessageHeaders serviceMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader);

        void CallServiceFailed(Uri requestUri, string serviceMethodName, ServiceRemotingMessageHeaders serviceMessageHeaders, CustomServiceRequestHeader customServiceRequestHeader, Exception ex);


        void ServiceClientFailed(Uri requestUri, CustomServiceRequestHeader customServiceRequestHeader, Exception ex);

    }
}