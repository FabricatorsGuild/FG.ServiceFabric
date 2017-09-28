using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.V1;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public static class ServiceRemotingMessageHeadersExtensions
    {
        public static void AddHeaders(this ServiceRemotingMessageHeaders messageHeaders, IEnumerable<ServiceRequestHeader> headers)
        {
            foreach (var header in headers)
            {
                messageHeaders.AddHeader(header);
            }
        }

        public static void AddHeader(this ServiceRemotingMessageHeaders messageHeaders, ServiceRequestHeader header)
        {
            messageHeaders.AddHeader(header.HeaderName, header.GetValue());
        }

	    public static bool HasHeader(this ServiceRemotingMessageHeaders messageHeaders, string headerName)
	    {			
			return messageHeaders.TryGetHeaderValue(headerName, out byte[] value);
		}
    }
}