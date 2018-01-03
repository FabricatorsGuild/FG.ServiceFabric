namespace FG.ServiceFabric.Actors.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;

    using FG.Common.Expressions;
    using FG.ServiceFabric.Actors.Remoting.FabricTransport;
    using FG.ServiceFabric.Diagnostics;
    using FG.ServiceFabric.Services.Remoting.FabricTransport;
    using FG.ServiceFabric.Services.Remoting.Runtime.Client;

    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Builder;
    using Microsoft.ServiceFabric.Services.Remoting.V1;
    using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

    public class ActorProxyFactory : ServiceProxyFactoryBase, IActorProxyFactory
    {
        private static readonly object _lock = new object();

        private static readonly ConcurrentDictionary<Type, MethodDispatcherBase> ActorMethodDispatcherMap = new ConcurrentDictionary<Type, MethodDispatcherBase>();

        private static readonly Func<Type, MethodDispatcherBase> GetOrCreateActorMethodDispatcher;

        private static volatile Func<ActorProxyFactory, Type, Type, IActorProxyFactory> _actorProxyFactoryInnerFactory;

        private IActorProxyFactory _innerActorProxyFactory;

        static ActorProxyFactory()
        {
            SetInnerFactory(null);
            GetOrCreateActorMethodDispatcher =
                MethodCallProxyFactory.CreateMethodProxyFactory.CreateMethodProxy<Type, MethodDispatcherBase>(GetGetOrCreateActorMethodDispatcherMethodInfo(), "type");
        }

        public ActorProxyFactory(IActorClientLogger logger)
            : base(logger)
        {
            this.Logger = logger;
        }

        private IActorClientLogger Logger { get; }

        public TActorInterface CreateActorProxy<TActorInterface>(ActorId actorId, string applicationName = null, string serviceName = null, string listenerName = null)
            where TActorInterface : IActor
        {
            GetOrDiscoverActorMethodDispatcher(typeof(TActorInterface));
            var proxy = this.GetInnerActorProxyFactory(null, typeof(TActorInterface)).CreateActorProxy<TActorInterface>(actorId, applicationName, serviceName, listenerName);
            var serviceUri = ((IActorProxy)proxy).ActorServicePartitionClient.ServiceUri;
            UpdateRequestContext(serviceUri);
            return proxy;
        }

        public TActorInterface CreateActorProxy<TActorInterface>(Uri serviceUri, ActorId actorId, string listenerName = null)
            where TActorInterface : IActor
        {
            GetOrDiscoverActorMethodDispatcher(typeof(TActorInterface));
            var proxy = this.GetInnerActorProxyFactory(null, typeof(TActorInterface)).CreateActorProxy<TActorInterface>(serviceUri, actorId, listenerName);
            UpdateRequestContext(serviceUri);
            return proxy;
        }

        public TServiceInterface CreateActorServiceProxy<TServiceInterface>(Uri serviceUri, ActorId actorId, string listenerName = null)
            where TServiceInterface : IService
        {
            var partitionKey = actorId.GetPartitionKey();
            return this.CreateActorServiceProxy<TServiceInterface>(serviceUri, partitionKey, listenerName);
        }

        public TServiceInterface CreateActorServiceProxy<TServiceInterface>(Uri serviceUri, long partitionKey, string listenerName = null)
            where TServiceInterface : IService
        {
            this.GetOrDiscoverServiceMethodDispatcher(typeof(TServiceInterface));
            var proxy = this.GetInnerServiceProxyFactory(typeof(TServiceInterface))
                .CreateServiceProxy<TServiceInterface>(serviceUri, new ServicePartitionKey(partitionKey), TargetReplicaSelector.Default, listenerName);
            UpdateRequestContext(serviceUri);
            return proxy;
        }

        internal static MethodDispatcherBase GetOrDiscoverActorMethodDispatcher(Type actorInterfaceType)
        {
            if (actorInterfaceType == null)
            {
                return null;
            }

            return ActorMethodDispatcherMap.GetOrAdd(actorInterfaceType, GetActorMethodInformation);
        }

        internal static void SetInnerFactory(Func<ActorProxyFactory, Type, Type, IActorProxyFactory> innerFactory)
        {
            if (innerFactory == null)
            {
                innerFactory = (actorProxyFactory, serviceInterfaceType, actorInterfaceType) =>
                    new Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory(
                        client => actorProxyFactory.CreateServiceRemotingClientFactory(client, serviceInterfaceType, actorInterfaceType));
            }

            _actorProxyFactoryInnerFactory = innerFactory;
        }

        private static MethodDispatcherBase GetActorMethodInformation(Type actorInterfaceType)
        {
            return GetOrCreateActorMethodDispatcher(actorInterfaceType);
        }

        private static MethodInfo GetGetOrCreateActorMethodDispatcherMethodInfo()
        {
            var codeBuilderType =
                typeof(Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory).Assembly.GetType("Microsoft.ServiceFabric.Actors.Remoting.V1.Builder.ActorCodeBuilder");
            return codeBuilderType?.GetMethod("GetOrCreateMethodDispatcher", BindingFlags.Public | BindingFlags.Static);
        }

        private IServiceRemotingClientFactory CreateServiceRemotingClientFactory(
            IServiceRemotingCallbackClient serviceRemotingCallbackClient,
            Type serviceInterfaceType,
            Type actorInterfaceType)
        {
            var serviceMethodDispatcher = this.GetOrDiscoverServiceMethodDispatcher(serviceInterfaceType);
            var actorMethodDispatcher = GetOrDiscoverActorMethodDispatcher(actorInterfaceType);
            var actorServiceMethodDispatcher = this.GetOrDiscoverServiceMethodDispatcher(typeof(IActorService));

            var contextWrapper = ServiceRequestContextWrapper.Current;
            var interfaceType = actorInterfaceType ?? serviceInterfaceType;
            return FabricTransportActorRemotingHelpers.CreateServiceRemotingClientFactory(
                interfaceType,
                serviceRemotingCallbackClient,
                this.Logger,
                contextWrapper.CorrelationId,
                new[] { actorMethodDispatcher, serviceMethodDispatcher, actorServiceMethodDispatcher });
        }

        private IActorProxyFactory GetInnerActorProxyFactory(Type serviceInterfaceType, Type actorInterfaceType)
        {
            if (this._innerActorProxyFactory != null)
            {
                return this._innerActorProxyFactory;
            }

            lock (_lock)
            {
                return this._innerActorProxyFactory = _actorProxyFactoryInnerFactory(this, serviceInterfaceType, actorInterfaceType);
            }
        }
    }
}