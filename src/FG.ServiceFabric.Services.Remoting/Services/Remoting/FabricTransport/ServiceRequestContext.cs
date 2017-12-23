namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Runtime.Remoting.Messaging;

    /// <summary>
    ///     Provides a handler and a container for managing the current logical call context values
    /// </summary>
    public sealed class ServiceRequestContext
    {
        private static readonly string ContextKey = Guid.NewGuid().ToString();

        private ServiceRequestContext()
        {
        }

        /// <summary>
        ///     Gets the current service request context
        /// </summary>
        public static ServiceRequestContext Current { get; } = new ServiceRequestContext();

        /// <summary>
        ///     Gets all property keys in the current call context
        /// </summary>
        public IEnumerable<string> Keys => this.CurrentRequestContextData?.Properties.Keys ?? Enumerable.Empty<string>();

        /// <summary>
        ///     Gets all properties in the current call context
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties => this.CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, string>.Empty;

        internal ImmutableDictionary<string, string> InternalProperties
        {
            get => this.CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, string>.Empty;
            set => this.CurrentRequestContextData = new RequestContextData(value);
        }

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

        /// <summary>
        ///     Gets or sets a service context value
        /// </summary>
        /// <param name="key">The property name/key</param>
        /// <returns>The string value of the property (or null - when the property does not exist or the value is null</returns>
        public string this[string key]
        {
            get
            {
                var dataObject = this.CurrentRequestContextData;

                if (dataObject == null || dataObject.Properties.TryGetValue(key, out var value) == false)
                {
                    return null;
                }

                return value;
            }

            set => this.Update(key, value, (i, v, d) => d.SetItem(i, v));
        }

        /// <summary>
        ///     Clears the service request context
        /// </summary>
        /// <returns>A service request context object</returns>
        public ServiceRequestContext Clear()
        {
            this.CurrentRequestContextData = null;
            return this;
        }

        /// <summary>
        ///     Updates the service request context properties
        /// </summary>
        /// <param name="propertyUpdateFunc">The method that updates the service request dictionary</param>
        /// <returns>A service request context object</returns>
        public ServiceRequestContext Update(Func<ImmutableDictionary<string, string>, ImmutableDictionary<string, string>> propertyUpdateFunc)
        {
            this.CurrentRequestContextData = new RequestContextData(propertyUpdateFunc(this.CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, string>.Empty));
            return this;
        }

        /// <summary>
        ///     Updates the service request context properties, passing a value, which in turn saves a heap allocation by avoiding
        ///     a closure
        /// </summary>
        /// <typeparam name="T">The parameter type</typeparam>
        /// <param name="value">The value to pass to the update property method</param>
        /// <param name="propertyUpdateFunc">The method that updates the service request dictionary</param>
        /// <returns>A service request context object</returns>
        public ServiceRequestContext Update<T>(T value, Func<T, ImmutableDictionary<string, string>, ImmutableDictionary<string, string>> propertyUpdateFunc)
        {
            this.CurrentRequestContextData =
                new RequestContextData(propertyUpdateFunc(value, this.CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, string>.Empty));
            return this;
        }

        /// <summary>
        ///     Updates the service request context properties, passing 2 values, which in turn saves a heap allocation by avoiding
        ///     a closure
        /// </summary>
        /// <typeparam name="T1">The first parameter type</typeparam>
        /// <typeparam name="T2">The second parameter type</typeparam>
        /// <param name="value1">The first value to pass to the update property method</param>
        /// <param name="value2">The second value to pass to the update property method</param>
        /// <param name="propertyUpdateFunc">The method that updates the service request dictionary</param>
        /// <returns>A service request context object</returns>
        public ServiceRequestContext Update<T1, T2>(T1 value1, T2 value2, Func<T1, T2, ImmutableDictionary<string, string>, ImmutableDictionary<string, string>> propertyUpdateFunc)
        {
            this.CurrentRequestContextData =
                new RequestContextData(propertyUpdateFunc(value1, value2, this.CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, string>.Empty));
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