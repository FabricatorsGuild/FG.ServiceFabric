namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Runtime.Remoting.Messaging;

    public sealed class ServiceRequestContext
    {
        private static readonly string ContextKey = Guid.NewGuid().ToString();

        private ServiceRequestContext()
        {
        }

        public static ServiceRequestContext Current { get; } = new ServiceRequestContext();

        public IEnumerable<string> Keys => this.CurrentRequestContextData?.Properties.Keys ?? Enumerable.Empty<string>();

        public IReadOnlyDictionary<string, string> Properties => this.CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, string>.Empty;

        private RequestContextData CurrentRequestContextData
        {
            get => CallContext.LogicalGetData(ContextKey) as RequestContextData;

            set
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

        public string this[string index]
        {
            get
            {
                var dataObject = this.CurrentRequestContextData;

                if (dataObject == null || dataObject.Properties.TryGetValue(index, out var value) == false)
                {
                    return null;
                }

                return value;
            }

            set => this.Update(d => d.SetItem(index, value));
        }

        public ServiceRequestContext Clear()
        {
            this.CurrentRequestContextData = null;
            return this;
        }

        public ServiceRequestContext Update(Func<ImmutableDictionary<string, string>, ImmutableDictionary<string, string>> propertyUpdateFunc)
        {
            this.CurrentRequestContextData = new RequestContextData(propertyUpdateFunc(this.CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, string>.Empty));
            return this;
        }

        private sealed class RequestContextData : MarshalByRefObject
        {
            public RequestContextData(ImmutableDictionary<string, string> properties)
            {
                this.Properties = properties;
            }

            public RequestContextData()
            {
                this.Properties = ImmutableDictionary<string, string>.Empty;
            }

            public ImmutableDictionary<string, string> Properties { get; }
        }
    }
}