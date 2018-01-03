using System;
using System.Collections.Generic;
using System.Reflection;
using Castle.DynamicProxy;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    public class MockActorServiceProxy : MockServiceProxy
    {
        public MockActorServiceProxy(
            object target,
            Uri serviceUri,
            Type serviceInterfaceType,
            ServicePartitionKey partitionKey,
            TargetReplicaSelector replicaSelector,
            string listenerName,
            ICommunicationClientFactory<IServiceRemotingClient> factory, IMockServiceProxyManager serviceProxyManager) :
            base(
                target,
                serviceUri,
                serviceInterfaceType,
                partitionKey,
                replicaSelector,
                listenerName,
                factory,
                serviceProxyManager)
        {
        }

        protected override IEnumerable<IInterceptor> GetInterceptors(object target, Type serviceInterfaceType,
            IMockServiceProxyManager serviceProxyManager)
        {
            if (target is ActorService mockActorService)
                yield return new ActorServiceInterceptor(mockActorService);
            foreach (var interceptor in base.GetInterceptors(target, serviceInterfaceType, serviceProxyManager))
                yield return interceptor;
        }

        private class ActorServiceInterceptor : IInterceptor, IInterceptorFilter
        {
            private readonly MockActorServiceExtension _mockActorService;

            public ActorServiceInterceptor(ActorService actorService)
            {
                _mockActorService = new MockActorServiceExtension(actorService);
            }

            public void Intercept(IInvocation invocation)
            {
                invocation.ReturnValue = invocation.Method.Invoke(_mockActorService, invocation.Arguments);
            }

            public bool ShouldIntercept(Type type, MethodInfo method, IEnumerable<IInterceptor> acceptedInterceptors)
            {
                return method.DeclaringType == typeof(IActorService) &&
                       method.Name == nameof(IActorService.DeleteActorAsync);
            }
        }
    }
}