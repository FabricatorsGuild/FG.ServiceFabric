using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    public class MockServiceProxy : IServiceProxy
    {
        private readonly MockServiceInstanceProxyCache instanceProxyCache = new MockServiceInstanceProxyCache();

        public MockServiceProxy(
            object target,
            Uri serviceUri,
            Type serviceInterfaceType,
            ServicePartitionKey partitionKey,
            TargetReplicaSelector replicaSelector,
            string listenerName,
            ICommunicationClientFactory<IServiceRemotingClient> factory,
            IMockServiceProxyManager serviceProxyManager)
        {
            ServiceInterfaceType = serviceInterfaceType;
            ServicePartitionClient =
                new MockServicePartitionClient(serviceUri, partitionKey, replicaSelector, listenerName, factory);

            this.Proxy = this.instanceProxyCache.GetOrAdd(new MockServiceInstanceProxyCacheKey(serviceUri, serviceInterfaceType, partitionKey), k => this.CreateDynamicProxy(target, serviceInterfaceType, serviceProxyManager));
        }

        public object Proxy { get; }


        public Type ServiceInterfaceType { get; }
        public IServiceRemotingPartitionClient ServicePartitionClient { get; }

        public Microsoft.ServiceFabric.Services.Remoting.V2.Client.IServiceRemotingPartitionClient
            ServicePartitionClient2 { get; }

        protected virtual IEnumerable<IInterceptor> GetInterceptors(object target, Type serviceInterfaceType,
            IMockServiceProxyManager serviceProxyManager)
        {
            var serviceProxyInterceptor = new ServiceProxyInterceptor(this);
            var serviceInterceptor = new ServiceInterceptor(serviceProxyManager);

            return new IInterceptor[] {serviceInterceptor, serviceProxyInterceptor};
        }

        protected virtual IInterceptorSelector GetInterceptorSelector(object target, Type serviceInterfaceType,
            IMockServiceProxyManager serviceProxyManager)
        {
            var selector = new InterceptorSelector();
            return selector;
        }

        protected object CreateDynamicProxy(object target, Type serviceInterfaceType,
            IMockServiceProxyManager serviceProxyManager)
        {
            var generator = new ProxyGenerator(new PersistentProxyBuilder());
            var selector = GetInterceptorSelector(target, serviceInterfaceType, serviceProxyManager);
            var interceptors = GetInterceptors(target, serviceInterfaceType, serviceProxyManager);
            var options = new ProxyGenerationOptions {Selector = selector};
            var proxy = generator.CreateInterfaceProxyWithTarget(
                serviceInterfaceType,
                new[] {typeof(IServiceProxy)},
                target,
                options,
                interceptors.ToArray());
            return proxy;
        }

        private class MockServicePartitionClient : IServiceRemotingPartitionClient
        {
            internal MockServicePartitionClient(
                Uri serviceUri,
                ServicePartitionKey partitionKey,
                TargetReplicaSelector replicaSelector,
                string listenerName,
                ICommunicationClientFactory<IServiceRemotingClient> factory)
            {
                ServiceUri = serviceUri;
                PartitionKey = partitionKey;
                TargetReplicaSelector = replicaSelector;
                ListenerName = listenerName;
                Factory = factory;
            }

            public bool TryGetLastResolvedServicePartition(out ResolvedServicePartition resolvedServicePartition)
            {
                throw new NotImplementedException();
            }

            public Uri ServiceUri { get; }
            public ServicePartitionKey PartitionKey { get; }
            public TargetReplicaSelector TargetReplicaSelector { get; }
            public string ListenerName { get; }
            public ICommunicationClientFactory<IServiceRemotingClient> Factory { get; }
        }


        private class InterceptorSelector : IInterceptorSelector
        {
            [DebuggerStepThrough]
            public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
            {
                var acceptedInterceptors = new List<IInterceptor>();
                foreach (var interceptor in interceptors)
                    if ((interceptor as IInterceptorFilter)?.ShouldIntercept(type, method, acceptedInterceptors) ??
                        false)
                        acceptedInterceptors.Add(interceptor);

                return acceptedInterceptors.ToArray();
            }
        }

        protected interface IInterceptorFilter
        {
            bool ShouldIntercept(Type type, MethodInfo method, IEnumerable<IInterceptor> acceptedInterceptors);
        }

        private class ServiceInterceptor : IInterceptor, IInterceptorFilter
        {
            private readonly IMockServiceProxyManager _serviceProxyManager;

            public ServiceInterceptor(IMockServiceProxyManager serviceProxyManager)
            {
                _serviceProxyManager = serviceProxyManager;
            }

            [DebuggerStepThrough]
            public void Intercept(IInvocation invocation)
            {
                _serviceProxyManager?.BeforeMethod(invocation.Proxy as IService, invocation.Method);

                invocation.Proceed();
                if (invocation.ReturnValue is Task returnTask)
                {
                    returnTask.ContinueWith(t => _serviceProxyManager?.AfterMethod(invocation.Proxy as IService, invocation.Method));
                }
                else
                {
                    _serviceProxyManager?.AfterMethod(invocation.Proxy as IService, invocation.Method);
                }
            }

            public bool ShouldIntercept(Type type, MethodInfo method, IEnumerable<IInterceptor> acceptedInterceptors)
            {
                return !acceptedInterceptors.Any();
            }
        }

        protected class ServiceProxyInterceptor : IInterceptor, IInterceptorFilter
        {
            private readonly IServiceProxy _serviceProxy;

            public ServiceProxyInterceptor(IServiceProxy serviceProxy)
            {
                _serviceProxy = serviceProxy;
            }

            public void Intercept(IInvocation invocation)
            {
                invocation.ReturnValue = invocation.Method.Invoke(_serviceProxy, invocation.Arguments);
            }

            public bool ShouldIntercept(Type type, MethodInfo method, IEnumerable<IInterceptor> acceptedInterceptors)
            {
                return method.DeclaringType == typeof(IServiceProxy);
            }
        }
    }
}