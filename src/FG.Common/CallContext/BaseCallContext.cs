using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FG.Common.CallContext
{
    public class BaseCallContext<TImplementation, TValueType>
        where TImplementation : class
    {
        private readonly string _contextKey = Guid.NewGuid().ToString();

        /// <summary>
        ///     Gets all property keys in the current call context
        /// </summary>
        public IEnumerable<string> Keys => CurrentRequestContextData?.Properties.Keys ?? Enumerable.Empty<string>();

        /// <summary>
        ///     Gets all properties in the current call context
        /// </summary>
        public IReadOnlyDictionary<string, TValueType> Properties =>
            CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, TValueType>.Empty;

        internal ImmutableDictionary<string, TValueType> InternalProperties
        {
            get => CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, TValueType>.Empty;
            set => CurrentRequestContextData = new RequestContextData(value);
        }

        private RequestContextData CurrentRequestContextData
        {
            get => System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(_contextKey) as RequestContextData;

            set
            {
                if (value == null)
                    System.Runtime.Remoting.Messaging.CallContext.FreeNamedDataSlot(_contextKey);
                else
                    System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(_contextKey, value);
            }
        }

        /// <summary>
        ///     Gets or sets a service context value
        /// </summary>
        /// <param name="key">The property name/key</param>
        /// <returns>The string value of the property (or null - when the property does not exist or the value is null</returns>
        public TValueType this[string key]
        {
            get
            {
                var dataObject = CurrentRequestContextData;

                if (dataObject == null || dataObject.Properties.TryGetValue(key, out var value) == false)
                    return default(TValueType);

                return value;
            }

            set => Update(key, value, (i, v, d) => d.SetItem(i, v));
        }

        public TImplementation SetItem(string key, TValueType value)
        {
            return Update(key, value, (k, v, d) => d.SetItem(k, v));
        }

        public TValueType GetItem(string key)
        {
            return this[key];
        }

        /// <summary>
        ///     Clears the service request context
        /// </summary>
        /// <returns>A service request context object</returns>
        public TImplementation Clear()
        {
            CurrentRequestContextData = null;
            return this as TImplementation;
        }

        /// <summary>
        ///     Updates the service request context properties
        /// </summary>
        /// <param name="propertyUpdateFunc">The method that updates the service request dictionary</param>
        /// <returns>A service request context object</returns>
        public TImplementation Update(
            Func<ImmutableDictionary<string, TValueType>, ImmutableDictionary<string, TValueType>> propertyUpdateFunc)
        {
            CurrentRequestContextData = new RequestContextData(
                propertyUpdateFunc(CurrentRequestContextData?.Properties ??
                                   ImmutableDictionary<string, TValueType>.Empty));
            return this as TImplementation;
        }

        /// <summary>
        ///     Updates the service request context properties, passing a value, which in turn saves a heap allocation by avoiding
        ///     a closure
        /// </summary>
        /// <typeparam name="T">The parameter type</typeparam>
        /// <param name="value">The value to pass to the update property method</param>
        /// <param name="propertyUpdateFunc">The method that updates the service request dictionary</param>
        /// <returns>A service request context object</returns>
        public TImplementation Update<T>(T value,
            Func<T, ImmutableDictionary<string, TValueType>, ImmutableDictionary<string, TValueType>>
                propertyUpdateFunc)
        {
            CurrentRequestContextData =
                new RequestContextData(propertyUpdateFunc(value,
                    CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, TValueType>.Empty));
            return this as TImplementation;
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
        public TImplementation Update<T1, T2>(T1 value1, T2 value2,
            Func<T1, T2, ImmutableDictionary<string, TValueType>, ImmutableDictionary<string, TValueType>>
                propertyUpdateFunc)
        {
            CurrentRequestContextData =
                new RequestContextData(propertyUpdateFunc(value1, value2,
                    CurrentRequestContextData?.Properties ?? ImmutableDictionary<string, TValueType>.Empty));
            return this as TImplementation;
        }

        private sealed class RequestContextData : MarshalByRefObject
        {
            public RequestContextData(ImmutableDictionary<string, TValueType> properties)
            {
                Properties = properties;
            }

            public RequestContextData()
            {
                Properties = ImmutableDictionary<string, TValueType>.Empty;
            }

            public ImmutableDictionary<string, TValueType> Properties { get; }
        }
    }
}