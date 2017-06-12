using System;
using System.Collections.Concurrent;
using System.Reflection;
using FG.ServiceFabric.Actors.Remoting.FabricTransport;
using FG.ServiceFabric.Diagnostics;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using FG.ServiceFabric.Services.Remoting.Runtime.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Builder;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Actors.Client
{ 
    public class ActorProxyFactory : ServiceProxyFactoryBase, IActorProxyFactory
    {
        private readonly object _lock = new object();

        private static readonly ConcurrentDictionary<Type, IActorProxyFactory> ActorProxyFactoryMap = new ConcurrentDictionary<Type, IActorProxyFactory>();
        private static readonly ConcurrentDictionary<Type, MethodDispatcherBase> ActorMethodDispatcherMap = new ConcurrentDictionary<Type, MethodDispatcherBase>();

        private IActorClientLogger Logger { get; set; }

        public ActorProxyFactory(IActorClientLogger logger)
        {
            Logger = logger;
        }

        private IServiceRemotingClientFactory CreateServiceRemotingClientFactory(IServiceRemotingCallbackClient serviceRemotingCallbackClient, Type serviceInterfaceType, Type actorInterfaceType)
        {
            var serviceMethodDispatcher = base.GetOrDiscoverServiceMethodDispatcher(serviceInterfaceType);
            var actorMethodDispatcher = GetOrDiscoverActorMethodDispatcher(actorInterfaceType);

            var interfaceType = actorInterfaceType ?? serviceInterfaceType;
            return FabricTransportActorRemotingHelpers.CreateServiceRemotingClientFactory(
                interfaceType, 
                serviceRemotingCallbackClient, 
                Logger,
                ServiceRequestContext.Current?[ServiceRequestContextKeys.CorrelationId],
                serviceMethodDispatcher,
                actorMethodDispatcher);
        }

        private IActorProxyFactory GetInnerActorProxyFactory(Type serviceInterfaceType, Type actorInterfaceType)
        {
            var interfaceType = actorInterfaceType ?? serviceInterfaceType;
            if (ActorProxyFactoryMap.ContainsKey(interfaceType))
            {
                return ActorProxyFactoryMap[interfaceType];
            }

            lock (_lock)
            {
                if (ActorProxyFactoryMap.ContainsKey(interfaceType))
                {
                    return ActorProxyFactoryMap[interfaceType];
                }
                var innerActorProxyFactory = new Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory(client => CreateServiceRemotingClientFactory(client, serviceInterfaceType, actorInterfaceType));
                ActorProxyFactoryMap[interfaceType] = innerActorProxyFactory;
                
                return innerActorProxyFactory;
            }
        }

        private MethodDispatcherBase GetOrDiscoverActorMethodDispatcher(Type actorInterfaceType)
        {
            if (actorInterfaceType == null) return null;

            if (ActorMethodDispatcherMap.ContainsKey(actorInterfaceType))
            {
                return ActorMethodDispatcherMap[actorInterfaceType];
            }

            lock (_lock)
            {
                if (ActorMethodDispatcherMap.ContainsKey(actorInterfaceType))
                {
                    return ActorMethodDispatcherMap[actorInterfaceType];
                }
                var actorMethodDispatcher = GetActorMethodInformation(actorInterfaceType);
                ActorMethodDispatcherMap[actorInterfaceType] = actorMethodDispatcher;

                return actorMethodDispatcher;
            }
        }


        public TActorInterface CreateActorProxy<TActorInterface>(ActorId actorId, string applicationName = null, string serviceName = null, string listenerName = null) where TActorInterface : IActor
        {
            GetOrDiscoverActorMethodDispatcher(typeof(TActorInterface));
            var proxy = GetInnerActorProxyFactory(null, typeof(TActorInterface)).CreateActorProxy<TActorInterface>(actorId, applicationName, serviceName, listenerName);
            UpdateRequestContext(proxy.GetActorReference().ServiceUri);
            return proxy;
        }

        public TActorInterface CreateActorProxy<TActorInterface>(Uri serviceUri, ActorId actorId, string listenerName = null) where TActorInterface : IActor
        {
            GetOrDiscoverActorMethodDispatcher(typeof(TActorInterface));
            var proxy = GetInnerActorProxyFactory(null, typeof(TActorInterface)).CreateActorProxy<TActorInterface>(serviceUri, actorId, listenerName);
            UpdateRequestContext(proxy.GetActorReference().ServiceUri);
            return proxy;
        }

        public TServiceInterface CreateActorServiceProxy<TServiceInterface>(Uri serviceUri, ActorId actorId, string listenerName = null) where TServiceInterface : IService
        {
            GetOrDiscoverServiceMethodDispatcher(typeof(TServiceInterface));
            var proxy = GetInnerActorProxyFactory(typeof(TServiceInterface), null).CreateActorServiceProxy<TServiceInterface>(serviceUri, actorId, listenerName);
            UpdateRequestContext(serviceUri);
            return proxy;
        }

        public TServiceInterface CreateActorServiceProxy<TServiceInterface>(Uri serviceUri, long partitionKey, string listenerName = null) where TServiceInterface : IService
        {
            GetOrDiscoverServiceMethodDispatcher(typeof(TServiceInterface));
            var proxy = GetInnerActorProxyFactory(typeof(TServiceInterface), null).CreateActorServiceProxy<TServiceInterface>(serviceUri, partitionKey, listenerName);
            UpdateRequestContext(serviceUri);
            return proxy;
        }

        private static MethodDispatcherBase GetActorMethodInformation(Type actorInterfaceType )
        {
            var codeBuilderType = typeof(Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory)?.Assembly.GetType(
                "Microsoft.ServiceFabric.Actors.Remoting.Builder.ActorCodeBuilder");

            var getOrCreateMethodDispatcher = codeBuilderType?.GetMethod("GetOrCreateMethodDispatcher", BindingFlags.Public | BindingFlags.Static);
            var methodDispatcherBase = getOrCreateMethodDispatcher?.Invoke(null, new object[] { actorInterfaceType }) as MethodDispatcherBase;

            return methodDispatcherBase;       
        }
    }
}