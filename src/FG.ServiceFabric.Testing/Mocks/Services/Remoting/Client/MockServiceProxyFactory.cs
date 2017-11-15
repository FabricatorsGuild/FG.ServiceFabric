// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Testing.Mocks.Data;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
	/// <summary>
	///     Wrapper class for the static ServiceProxy.
	/// </summary>
	public class MockServiceProxyFactory : IServiceProxyFactory, IMockServiceProxyManager
	{
		private readonly MockFabricRuntime _fabricRuntime;

		public MockServiceProxyFactory(MockFabricRuntime fabricRuntime)
		{
			_fabricRuntime = fabricRuntime;
		}

		void IMockServiceProxyManager.BeforeMethod(IService service, MethodInfo method)
		{
			if (!_fabricRuntime.DisableMethodCallOutput)
			{
				Console.WriteLine();
				var color = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Green;
				var message = $"Service {service?.GetType().Name} ({service?.GetHashCode()}) {method} activating";
				Console.WriteLine($"{message.PadRight(80, '=')}");
				Console.ForegroundColor = color;
			}
		}

		void IMockServiceProxyManager.AfterMethod(IService service, MethodInfo method)
		{
			if (!_fabricRuntime.DisableMethodCallOutput)
			{
				var color = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				var message = $"Actor {service?.GetType().Name} ({service?.GetHashCode()}) {method} terminating";
				Console.WriteLine($"{message.PadRight(80, '=')}");
				Console.ForegroundColor = color;
				Console.WriteLine();
			}
		}

		public TServiceInterface CreateServiceProxy<TServiceInterface>(Uri serviceUri,
			ServicePartitionKey partitionKey = null,
			TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null)
			where TServiceInterface : IService
		{
			var serviceInterfaceType = typeof(TServiceInterface);
			var instance =
				_fabricRuntime.Instances.SingleOrDefault(i => i.Equals(serviceUri, serviceInterfaceType, partitionKey));

			if (instance == null)
			{
				throw new ArgumentException(
					$"A service with interface {serviceInterfaceType.Name} could not be found for address {serviceUri}");
			}

			var mockServiceProxy = new MockServiceProxy(instance.ServiceInstance, serviceUri, serviceInterfaceType, partitionKey,
				TargetReplicaSelector.Default, "", null, this);
			return (TServiceInterface) mockServiceProxy.Proxy;
		}
	}
}