using System;
using System.Collections.Generic;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.V1;

namespace FG.ServiceFabric.Diagnostics
{
    public interface IServiceRemotingLogger
    {
        void StartRequestContext(IEnumerable<ServiceRequestHeader> headers);
        void StopRequestContext(IEnumerable<ServiceRequestHeader> headers);
        void FailedRequestContext(IEnumerable<ServiceRequestHeader> headers, Exception exception);

        void FailedToReadCustomServiceMessageHeader(ServiceRemotingMessageHeaders serviceRemotingMessageHeaders,
            Exception ex);
    }
}