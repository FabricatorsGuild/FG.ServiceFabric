using System;
using System.Collections.Concurrent;
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

        private static readonly ConcurrentDictionary<Type, IServiceProxyFactory> ServiceProxyFactoryMap = new ConcurrentDictionary<Type, IServiceProxyFactory>();

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
            if (ServiceProxyFactoryMap.ContainsKey(serviceInterfaceType))
            {
                return ServiceProxyFactoryMap[serviceInterfaceType];
            }

            lock (_lock)
            {
                if (ServiceProxyFactoryMap.ContainsKey(serviceInterfaceType))
                {
                    return ServiceProxyFactoryMap[serviceInterfaceType];
                }
                var innerActorProxyFactory = new Microsoft.ServiceFabric.Services.Remoting.Client.ServiceProxyFactory(
                            client => CreateServiceRemotingClientFactory(client, serviceInterfaceType));
                ServiceProxyFactoryMap[serviceInterfaceType] = innerActorProxyFactory;

                return innerActorProxyFactory;
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