// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Fabric;
using FG.Common.Utils;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    /// <summary>
    /// Wrapper class for the static ServiceProxy.
    /// </summary>
    public class MockServiceProxyFactory : IServiceProxyFactory
    {
        private readonly MockFabricRuntime _fabricRuntime;

        public MockServiceProxyFactory(MockFabricRuntime fabricRuntime)
        {
            _fabricRuntime = fabricRuntime;
        }

        private readonly ConcurrentDictionary<Uri, object> _mockServiceLookupTable = new ConcurrentDictionary<Uri, object>();

        public TServiceInterface CreateServiceProxy<TServiceInterface>(
            Uri serviceUri, ServicePartitionKey partitionKey = null, TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null)
            where TServiceInterface : IService
        {
            var serviceProxy = new MockServiceProxy();

	        Func<Uri, object> createServiceInstance = null;
	        if (_mockServiceLookupTable.ContainsKey(serviceUri))
	        {
				createServiceInstance = (uri) => _mockServiceLookupTable[uri];
	        }
	        else
	        {
				createServiceInstance = (uri) => _fabricRuntime.ActorProxyFactory.CreateActorServiceProxy<TServiceInterface>(uri, (partitionKey?.Value as Int64RangePartitionInformation)?.LowKey ?? 0, listenerName);
	        }
			serviceProxy.Supports<TServiceInterface>(createServiceInstance);

			var service = serviceProxy.Create<TServiceInterface>(serviceUri, partitionKey, targetReplicaSelector, listenerName);
            // ReSharper disable once SuspiciousTypeConversion.Global

            var target = (object) (service as FG.ServiceFabric.Services.Runtime.StatelessService) ??
                         service as FG.ServiceFabric.Services.Runtime.StatefulService;
            if (target != null)
            {
                target.SetPrivateField("_serviceProxyFactory", _fabricRuntime.ServiceProxyFactory);
                target.SetPrivateField("_actorProxyFactory", _fabricRuntime.ActorProxyFactory);
                target.SetPrivateField("_applicationUriBuilder", _fabricRuntime.ApplicationUriBuilder);
            }
            return service;
        }

        public void AssociateMockServiceAndName(Uri mockServiceUri, object mockService)
        {
            _mockServiceLookupTable.AddOrUpdate(mockServiceUri, mockService, (uri, service) => mockService);
        }

        public TService GetService<TService>(Uri serviceUri)
        {
            if( _mockServiceLookupTable.ContainsKey(serviceUri))
                return (TService)_mockServiceLookupTable[serviceUri];
            return default(TService);
        }
    }

    
}