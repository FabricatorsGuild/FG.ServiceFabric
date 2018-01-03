using System;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Remoting.V1.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    public class MockActorProxy : ActorProxy, IActorProxy
    {
        public MockActorProxy(
            object target,
            Type actorInterfaceType,
            ActorId actorId,
            Uri serviceUri,
            ServicePartitionKey partitionKey,
            TargetReplicaSelector replicaSelector,
            string listenerName,
            ICommunicationClientFactory<IServiceRemotingClient> factory,
            IMockActorProxyManager actorProxyManager)
        {
            ActorId = actorId;
            ActorServicePartitionClient =
                new MockActorServicePartitionClient(actorId, serviceUri, partitionKey, replicaSelector, listenerName,
                    factory);

            Proxy = CreateDynamicProxy(target, actorInterfaceType, actorProxyManager);
        }


        public object Proxy { get; }

        public new ActorId ActorId { get; }
        public new IActorServicePartitionClient ActorServicePartitionClient { get; }

        public new Microsoft.ServiceFabric.Actors.Remoting.V2.Client.IActorServicePartitionClient
            ActorServicePartitionClientV2 { get; }

        private object CreateDynamicProxy(object target, Type actorInterfaceType, IMockActorProxyManager actorManager)
        {
            var generator = new ProxyGenerator(new PersistentProxyBuilder());
            var selector = new InterceptorSelector();
            var actorInterceptor = new ActorInterceptor(actorManager);
            var actorProxyInterceptor = new ActorProxyInterceptor(this);
            var options = new ProxyGenerationOptions {Selector = selector};
            var proxy = generator.CreateInterfaceProxyWithTarget(
                actorInterfaceType,
                new[] {typeof(IActorProxy)},
                target,
                options,
                actorInterceptor,
                actorProxyInterceptor);
            return proxy;
        }

        protected override object GetReturnValue(int interfaceId, int methodId, object responseBody)
        {
            throw new NotImplementedException();
        }

        private class MockActorServicePartitionClient : IActorServicePartitionClient
        {
            internal MockActorServicePartitionClient(
                ActorId actorId,
                Uri serviceUri,
                ServicePartitionKey partitionKey,
                TargetReplicaSelector replicaSelector,
                string listenerName,
                ICommunicationClientFactory<IServiceRemotingClient> factory)
            {
                ActorId = actorId;
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
            public ActorId ActorId { get; }
        }

        private class InterceptorSelector : IInterceptorSelector
        {
            public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
            {
                if (method.DeclaringType == typeof(IActorProxy))
                    return interceptors.Where(i => i is ActorProxyInterceptor).ToArray();
                return interceptors.Where(i => i is ActorInterceptor).ToArray();
            }
        }

        private class ActorInterceptor : IInterceptor
        {
            private readonly IMockActorProxyManager _actorManager;

            public ActorInterceptor(IMockActorProxyManager actorManager)
            {
                _actorManager = actorManager;
            }

            public void Intercept(IInvocation invocation)
            {
                _actorManager?.BeforeMethod(invocation.Proxy as IActor, invocation.Method);
                invocation.Proceed();
                if (invocation.ReturnValue is Task returnTask)
                    returnTask.GetAwaiter().GetResult();

                _actorManager?.AfterMethod(invocation.Proxy as IActor, invocation.Method);
            }
        }

        private class ActorProxyInterceptor : IInterceptor
        {
            private readonly IActorProxy _actorProxy;

            public ActorProxyInterceptor(IActorProxy actorProxy)
            {
                _actorProxy = actorProxy;
            }

            public void Intercept(IInvocation invocation)
            {
                invocation.ReturnValue = invocation.Method.Invoke(_actorProxy, invocation.Arguments);
            }
        }
    }
}