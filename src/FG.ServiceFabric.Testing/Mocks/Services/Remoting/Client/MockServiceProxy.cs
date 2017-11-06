using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V1.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
	public class MockServiceProxy : IServiceProxy
	{
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
			ServicePartitionClient = new MockServicePartitionClient(serviceUri, partitionKey, replicaSelector, listenerName, factory);

			Proxy = CreateDynamicProxy(target, serviceInterfaceType, serviceProxyManager);
		}

		private object CreateDynamicProxy(object target, Type serviceInterfaceType, IMockServiceProxyManager serviceProxyManager)
		{
			var generator = new ProxyGenerator(new PersistentProxyBuilder());
			var selector = new InterceptorSelector();
			var serviceInterceptor = new ServiceInterceptor(serviceProxyManager);
			var serviceProxyInterceptor = new ServiceProxyInterceptor(this);
			var options = new ProxyGenerationOptions() {Selector = selector};
			var proxy = generator.CreateInterfaceProxyWithTarget(
				serviceInterfaceType,
				new Type[] {typeof(IServiceProxy)},
				target,
				options,
				serviceInterceptor,
				serviceProxyInterceptor);
			return proxy;
		}

		public object Proxy { get; }


		public Type ServiceInterfaceType { get; }
		public IServiceRemotingPartitionClient ServicePartitionClient { get; }
		public Microsoft.ServiceFabric.Services.Remoting.V2.Client.IServiceRemotingPartitionClient ServicePartitionClient2 { get; }

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
			[System.Diagnostics.DebuggerStepThrough]
			public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
			{
				if (method.DeclaringType == typeof(IActorProxy))
				{
					return interceptors.Where(i => (i is MockServiceProxy.ServiceProxyInterceptor)).ToArray();
				}
				return interceptors.Where(i => (i is MockServiceProxy.ServiceInterceptor)).ToArray();
			}
		}

		private class ServiceInterceptor : IInterceptor
		{
			private readonly IMockServiceProxyManager _serviceProxyManager;

			public ServiceInterceptor(IMockServiceProxyManager serviceProxyManager)
			{
				_serviceProxyManager = serviceProxyManager;
			}

			[System.Diagnostics.DebuggerStepThrough]
			public void Intercept(IInvocation invocation)
			{
				_serviceProxyManager?.BeforeMethod(invocation.Proxy as IService, invocation.Method);
				RunInvocation(invocation);
				_serviceProxyManager?.AfterMethod(invocation.Proxy as IService, invocation.Method);
			}

			public void RunInvocation(IInvocation invocation)
			{
				invocation.Proceed();
			}
		}

		private class ServiceProxyInterceptor : IInterceptor
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
		}
	}
}