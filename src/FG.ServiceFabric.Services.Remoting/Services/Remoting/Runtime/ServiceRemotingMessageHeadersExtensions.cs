using System;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Services.Remoting.Runtime
{
    public static class ServiceRemotingMessageHeadersExtensions
    {
        public static CustomServiceRequestHeader GetCustomServiceRequestHeader(this ServiceRemotingMessageHeaders messageHeaders, IServiceRemotingLogger logger)
        {
            try
            {
                CustomServiceRequestHeader customServiceRequestHeader;
                if (CustomServiceRequestHeader.TryFromServiceMessageHeaders(messageHeaders, out customServiceRequestHeader))
                {
                    return customServiceRequestHeader;
                }
            }
            catch (Exception ex)
            {
                // ignored
                logger?.FailedToReadCustomServiceMessageHeader(messageHeaders, ex);
            }
            return null;
        }
    }
}