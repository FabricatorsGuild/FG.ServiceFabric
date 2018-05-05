﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    using System;
    using System.Linq;
    using System.Reflection;

    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Microsoft.ServiceFabric.Services.Remoting;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    /// <summary>
    ///     Wrapper class for the static ServiceProxy.
    /// </summary>
    public class MockServiceProxyFactory : IServiceProxyFactory, IMockServiceProxyManager
    {
        private readonly MockFabricRuntime _fabricRuntime;

        public MockServiceProxyFactory(MockFabricRuntime fabricRuntime)
        {
            this._fabricRuntime = fabricRuntime;
        }

        public TServiceInterface CreateServiceProxy<TServiceInterface>(
            Uri serviceUri,
            ServicePartitionKey partitionKey = null,
            TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default,
            string listenerName = null)
            where TServiceInterface : IService
        {
            var serviceInterfaceType = typeof(TServiceInterface);
            var instance = this._fabricRuntime.Instances.SingleOrDefault(i => i.Equals(serviceUri, serviceInterfaceType, partitionKey));

            if (instance == null)
            {
                throw new ArgumentException($"A service with interface {serviceInterfaceType.Name} could not be found for address {serviceUri}");
            }

            var mockServiceProxy = new MockServiceProxy<TServiceInterface>(
                (TServiceInterface)instance.ServiceInstance,
                serviceUri,
                serviceInterfaceType,
                partitionKey,
                TargetReplicaSelector.Default,
                string.Empty,
                null,
                this);

            return (TServiceInterface)mockServiceProxy.Proxy;
        }

        void IMockServiceProxyManager.AfterMethod(IService service, MethodInfo method)
        {
            if (!this._fabricRuntime.DisableMethodCallOutput)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                var message = $"Actor {service?.GetType().Name} ({service?.GetHashCode()}) {method} terminating";
                Console.WriteLine($"{message.PadRight(80, '=')}");
                Console.ForegroundColor = color;
                Console.WriteLine();
            }
        }

        void IMockServiceProxyManager.BeforeMethod(IService service, MethodInfo method)
        {
            if (!this._fabricRuntime.DisableMethodCallOutput)
            {
                Console.WriteLine();
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                var message = $"Service {service?.GetType().Name} ({service?.GetHashCode()}) {method} activating";
                Console.WriteLine($"{message.PadRight(80, '=')}");
                Console.ForegroundColor = color;
            }
        }
    }
}