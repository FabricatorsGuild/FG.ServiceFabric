using System;
using System.Collections.Concurrent;
using System.Reflection;
using FG.Common.Expressions;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.Builder;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Services.Remoting.Runtime.Client
{
    /// <summary>
    ///     Provides a base class for service proxy factories
    /// </summary>
    public abstract class ServiceProxyFactoryBase
    {
        private static readonly Func<Type, MethodDispatcherBase> GetOrCreateServiceMethodDispatcher;

        private static readonly ConcurrentDictionary<Type, MethodDispatcherBase> ServiceMethodDispatcherMap =
            new ConcurrentDictionary<Type, MethodDispatcherBase>();

        private static volatile Func<ServiceProxyFactoryBase, Type, IServiceProxyFactory>
            _serviceProxyFactoryInnerFactory;

        private readonly object _lock = new object();

        private IServiceProxyFactory _innerProxyFactory;

        static ServiceProxyFactoryBase()
        {
            SetInnerFactory(null);
            GetOrCreateServiceMethodDispatcher =
                MethodCallProxyFactory.CreateMethodProxyFactory.CreateMethodProxy<Type, MethodDispatcherBase>(
                    GetGetOrCreateServiceMethodDispatcher(), "type");
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceProxyFactoryBase" /> class.
        /// </summary>
        /// <param name="logger">
        ///     A <see cref="IServiceClientLogger" />
        /// </param>
        protected ServiceProxyFactoryBase(IServiceClientLogger logger)
        {
            Logger = logger;
        }

        private IServiceClientLogger Logger { get; }

        internal static void SetInnerFactory(Func<ServiceProxyFactoryBase, Type, IServiceProxyFactory> innerFactory)
        {
            if (innerFactory == null)
                innerFactory = (serviceProxyFactory, serviceInterfaceType) =>
                    new Microsoft.ServiceFabric.Services.Remoting.Client.ServiceProxyFactory(
                        client => serviceProxyFactory.CreateServiceRemotingClientFactory(client, serviceInterfaceType));

            _serviceProxyFactoryInnerFactory = innerFactory;
        }

        /// <summary>
        ///     Updates the request context with a <see cref="Uri" />
        /// </summary>
        /// <param name="serviceUri">The current service uri</param>
        protected static void UpdateRequestContext(Uri serviceUri)
        {
            var contextWrapper = ServiceRequestContextWrapper.Current;

            contextWrapper.RequestUri = serviceUri?.ToString();
            if (contextWrapper.CorrelationId == null)
                contextWrapper.CorrelationId = Guid.NewGuid().ToString();
        }

        protected IServiceProxyFactory GetInnerServiceProxyFactory(Type serviceInterfaceType)
        {
            if (_innerProxyFactory != null)
                return _innerProxyFactory;

            lock (_lock)
            {
                return _innerProxyFactory = _serviceProxyFactoryInnerFactory(this, serviceInterfaceType);
            }
        }

        protected MethodDispatcherBase GetOrDiscoverServiceMethodDispatcher(Type serviceInterfaceType)
        {
            if (serviceInterfaceType == null)
                return null;

            return ServiceMethodDispatcherMap.GetOrAdd(serviceInterfaceType, GetOrCreateServiceMethodDispatcher);
        }

        private static MethodInfo GetGetOrCreateServiceMethodDispatcher()
        {
            var codeBuilderType =
                typeof(Microsoft.ServiceFabric.Services.Remoting.Client.ServiceProxyFactory)?.Assembly.GetType(
                    "Microsoft.ServiceFabric.Services.Remoting.V1.Builder.ServiceCodeBuilder");

            var getOrCreateMethodDispatcher = codeBuilderType?.GetMethod("GetOrCreateMethodDispatcher",
                BindingFlags.Public | BindingFlags.Static);
            return getOrCreateMethodDispatcher;
        }

        private IServiceRemotingClientFactory CreateServiceRemotingClientFactory(
            IServiceRemotingCallbackClient serviceRemotingCallbackClient, Type serviceInterfaceType)
        {
            var serviceMethodDispatcher = GetOrDiscoverServiceMethodDispatcher(serviceInterfaceType);

            var contextWrapper = ServiceRequestContextWrapper.Current;
            return FabricTransportServiceRemotingHelpers.CreateServiceRemotingClientFactory(
                serviceInterfaceType,
                serviceRemotingCallbackClient,
                Logger,
                contextWrapper.CorrelationId,
                serviceMethodDispatcher);
        }
    }
}