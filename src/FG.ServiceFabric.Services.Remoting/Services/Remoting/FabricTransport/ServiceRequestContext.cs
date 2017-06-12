using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using FG.ServiceFabric.Diagnostics;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public sealed class ServiceRequestContext
    {
        private static readonly string ContextKey = Guid.NewGuid().ToString();

        public ServiceRequestContext()
        {
            _values = new ConcurrentDictionary<string, string>();
        }

        public ServiceRequestContext(IEnumerable<ServiceRequestHeader> headers)
            : this()
        {
            Headers = headers;
        }

        public IServiceCommunicationLogger Logger { get; set; }

        public IEnumerable<ServiceRequestHeader> Headers { get; set; }

        public static void SetHeaders(IEnumerable<ServiceRequestHeader> headers)
        {
            Current.Headers = headers;
        }

        private readonly IDictionary<string, string> _values;

        public string this[string index]
        {
            get { return _values.ContainsKey(index) ? _values[index] : null; }
            set { _values[index] = value; }
        }

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