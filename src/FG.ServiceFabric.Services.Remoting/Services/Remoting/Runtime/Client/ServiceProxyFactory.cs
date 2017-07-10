using System;
using System.Collections.Concurrent;
using System.Fabric;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Services.Remoting.Runtime.Client
{
    public class ServiceProxyFactory : ServiceProxyFactoryBase, IServiceProxyFactory
    {
        private readonly object _lock = new object();
		private IServiceProxyFactory _innerProxyFactory;
		private static volatile Func<ServiceProxyFactory, Type, IServiceProxyFactory> _serviceProxyFactoryInnerFactory;

		static ServiceProxyFactory()
	    {
		    SetInnerFactory(null);
	    }

	    internal static void SetInnerFactory(Func<ServiceProxyFactory, Type, IServiceProxyFactory> innerFactory)
	    {
		    if (innerFactory == null)
		    {
			    innerFactory = (serviceProxyFactory, serviceInterfaceType) => new Microsoft.ServiceFabric.Services.Remoting.Client.ServiceProxyFactory(
					client => serviceProxyFactory.CreateServiceRemotingClientFactory(client, serviceInterfaceType));
		    }
		    _serviceProxyFactoryInnerFactory = innerFactory;
	    }

		private IServiceClientLogger Logger { get; set; }

        public ServiceProxyFactory(IServiceClientLogger logger)
        {
            Logger = logger;
        }

        private IServiceRemotingClientFactory CreateServiceRemotingClientFactory(IServiceRemotingCallbackClient serviceRemotingCallbackClient, Type serviceInterfaceType)
        {
            var serviceMethodDispatcher = base.GetOrDiscoverServiceMethodDispatcher(serviceInterfaceType);

            return FabricTransportServiceRemotingHelpers.CreateServiceRemotingClientFactory(
                 serviceInterfaceType,
                serviceRemotingCallbackClient,
                Logger,
                ServiceRequestContext.Current?[ServiceRequestContextKeys.CorrelationId],
                serviceMethodDispatcher);
        }

        private IServiceProxyFactory GetInnerServiceProxyFactory(Type serviceInterfaceType)
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

        public TServiceInterface CreateServiceProxy<TServiceInterface>(
            Uri serviceUri, 
            ServicePartitionKey partitionKey = null,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, 
            string listenerName = null
            ) where TServiceInterface : IService
        {
            GetOrDiscoverServiceMethodDispatcher(typeof(TServiceInterface));
            var proxy = GetInnerServiceProxyFactory(typeof(TServiceInterface)).CreateServiceProxy<TServiceInterface>(serviceUri, partitionKey, targetReplicaSelector, listenerName);
            UpdateRequestContext(serviceUri);
            return proxy;
        }
    }
}