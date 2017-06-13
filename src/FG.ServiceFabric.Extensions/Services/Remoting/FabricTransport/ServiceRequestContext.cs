using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public sealed class ServiceRequestContext
    {
        private static readonly string ContextKey = Guid.NewGuid().ToString();

        public ServiceRequestContext(IEnumerable<ServiceRequestHeader> headers)
        {
            Headers = headers;
        }
        public IEnumerable<ServiceRequestHeader> Headers { get; set; }

        public static ServiceRequestContext Current
        {
            get { return (ServiceRequestContext) CallContext.LogicalGetData(ContextKey); }
            internal set
            {
                if (value == null)
                {
                    CallContext.FreeNamedDataSlot(ContextKey);
                }
                else
                {
                    CallContext.LogicalSetData(ContextKey, value);
                }
            }
        }
    }
}