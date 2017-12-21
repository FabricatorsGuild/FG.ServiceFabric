namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.Remoting.Messaging;

    public sealed class ServiceRequestContext : MarshalByRefObject
    {
        private static readonly string ContextKey = Guid.NewGuid().ToString();

        private readonly IDictionary<string, string> _values;

        public ServiceRequestContext()
        {
            this._values = new ConcurrentDictionary<string, string>();
        }

        public static ServiceRequestContext Current
        {
            get => (ServiceRequestContext)CallContext.LogicalGetData(ContextKey);

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

        public IEnumerable<string> Keys => this._values.Keys;

        public string this[string index]
        {
            get => this._values.ContainsKey(index) ? this._values[index] : null;

            set => this._values[index] = value;
        }
    }
}