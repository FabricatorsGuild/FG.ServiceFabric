using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Fabric;
using System.Text;
using Microsoft.ServiceFabric.Services.Remoting;

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

        public static IEnumerable<ServiceRequestHeader> GetHeaders(this ServiceRemotingMessageHeaders messageHeaders, ServiceContext serviceContext, IService service)
        {
            var headers = new List<ServiceRequestHeader>();
            foreach (var serviceRequestHeader in ServiceRequestHeaderAttribute.GetServiceRequestHeader(serviceContext, service))
            {
                var headerData = messageHeaders.GetHeader(serviceRequestHeader);
                serviceRequestHeader.Value = headerData;
                headers.Add(serviceRequestHeader);
            }
            return headers;
        }

        public static byte[] GetHeader(this ServiceRemotingMessageHeaders messageHeaders, ServiceRequestHeader header)
        {
            byte[] headerValue = null;
            
            if (messageHeaders.TryGetHeaderValue(header.HeaderName, out headerValue))
            {
                return headerValue;
            }
            return null;
        }

        public static void AddHeader(this ServiceRemotingMessageHeaders messageHeaders, ServiceRequestHeader header)
        {
            messageHeaders.AddHeader(header.HeaderName, header.Value);
        }


        private const string UserHeaderName = @"user";
        private const string CorrelationIdHeaderName = @"correlationId";

        /// <summary>
        /// Reads the user header or returns null if empty
        /// </summary>
        /// <param name="messageHeaders">The headers</param>
        /// <returns>The set user name or null</returns>
        public static string GetUser(this ServiceRemotingMessageHeaders messageHeaders)
        {
            byte[] userHeader = null;
            string user = null;
            if (messageHeaders.TryGetHeaderValue(UserHeaderName, out userHeader))
            {
                // Deserialize and handle the header
                user = Encoding.UTF8.GetString(userHeader);
            }
            else
            {
                // Throw exception?
            }
            return user;
        }

        /// <summary>
        /// Reads the correlationId header or creates a new Guid if empty
        /// </summary>
        /// <param name="messageHeaders">The headers</param>
        /// <returns>The set correlation id or a new guid</returns>
        public static Guid GetCorrelationId(this ServiceRemotingMessageHeaders messageHeaders)
        {
            byte[] correlationIdHeader = null;
            Guid correlationId = Guid.NewGuid();
            if (messageHeaders.TryGetHeaderValue(CorrelationIdHeaderName, out correlationIdHeader))
            {
                // Deserialize and handle the header
                correlationId = new Guid(correlationIdHeader);
            }
            else
            {
                // Throw exception?
            }
            return correlationId;
        }

        /// <summary>
        /// Sets the user header
        /// </summary>
        /// <param name="messageHeaders"></param>
        /// <param name="user"></param>
        public static void SetUser(this ServiceRemotingMessageHeaders messageHeaders, string user)
        {
            byte[] userHeader = Encoding.UTF8.GetBytes(user);
            messageHeaders.AddHeader(UserHeaderName, userHeader);
        }

        /// <summary>
        /// Sets the correlationId header
        /// </summary>
        /// <param name="messageHeaders"></param>
        /// <param name="correlationId"></param>
        public static void SetCorrelationId(this ServiceRemotingMessageHeaders messageHeaders, Guid correlationId)
        {
            byte[] correlationIdHeader = correlationId.ToByteArray();
            messageHeaders.AddHeader(CorrelationIdHeaderName, correlationIdHeader);
        }
    }
}