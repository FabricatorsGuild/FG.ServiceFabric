using System;
using System.Collections.Concurrent;
using System.Reflection;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.Builder;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V1;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Services.Remoting.Runtime.Client
{
    public abstract class ServiceProxyFactoryBase
    {
        private static readonly ConcurrentDictionary<Type, MethodDispatcherBase> ServiceMethodDispatcherMap =
            new ConcurrentDictionary<Type, MethodDispatcherBase>();

        private static volatile Func<ServiceProxyFactoryBase, Type, IServiceProxyFactory> _serviceProxyFactoryInnerFactory;
        private readonly object _lock = new object();
        private IServiceProxyFactory _innerProxyFactory;

        static ServiceProxyFactoryBase()
        {
            SetInnerFactory(null);
        }

        protected ServiceProxyFactoryBase(IServiceClientLogger logger)
        {
            Logger = logger;
        }

        private IServiceClientLogger Logger { get; set; }

        internal static void SetInnerFactory(Func<ServiceProxyFactoryBase, Type, IServiceProxyFactory> innerFactory)
        {
            if (innerFactory == null)
            {
                innerFactory = (serviceProxyFactory, serviceInterfaceType) =>
                    new Microsoft.ServiceFabric.Services.Remoting.Client.ServiceProxyFactory(
                        client => serviceProxyFactory.CreateServiceRemotingClientFactory(client, serviceInterfaceType));
            }
            _serviceProxyFactoryInnerFactory = innerFactory;
        }

        protected MethodDispatcherBase GetOrDiscoverServiceMethodDispatcher(Type serviceInterfaceType)
        {
            if (serviceInterfaceType == null) return null;

            if (ServiceMethodDispatcherMap.ContainsKey(serviceInterfaceType))
            {
                return ServiceMethodDispatcherMap[serviceInterfaceType];
            }

            lock (_lock)
            {
                if (ServiceMethodDispatcherMap.ContainsKey(serviceInterfaceType))
                {
                    return ServiceMethodDispatcherMap[serviceInterfaceType];
                }
                var serviceMethodDispatcher = GetServiceMethodInformation(serviceInterfaceType);
                ServiceMethodDispatcherMap[serviceInterfaceType] = serviceMethodDispatcher;
                return serviceMethodDispatcher;
            }
        }

        private static MethodDispatcherBase GetServiceMethodInformation(Type serviceInterfaceType)
        {
            var codeBuilderType = typeof(Microsoft.ServiceFabric.Services.Remoting.Client.ServiceProxyFactory)?.Assembly.GetType(
                "Microsoft.ServiceFabric.Services.Remoting.V1.Builder.ServiceCodeBuilder");

            var getOrCreateMethodDispatcher =
                codeBuilderType?.GetMethod("GetOrCreateMethodDispatcher", BindingFlags.Public | BindingFlags.Static);
            var methodDispatcherBase =
                getOrCreateMethodDispatcher?.Invoke(null, new object[] { serviceInterfaceType }) as MethodDispatcherBase;

            return methodDispatcherBase;
        }

        protected static void UpdateRequestContext(Uri serviceUri)
        {
            if (ServiceRequestContext.Current == null) return;

            var contextWrapper = ServiceRequestContextWrapper.Current;

            contextWrapper.RequestUri = serviceUri?.ToString();
            if (contextWrapper.CorrelationId == null)
            {
                contextWrapper.CorrelationId = Guid.NewGuid().ToString();
            }
        }


        protected IServiceProxyFactory GetInnerServiceProxyFactory(Type serviceInterfaceType)
        {
            if (_innerProxyFactory != null)
            {
                return _innerProxyFactory;
            }

            lock (_lock)
            {
                _innerProxyFactory = _serviceProxyFactoryInnerFactory(this, serviceInterfaceType);

                return _innerProxyFactory;
            }
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