using System;
using System.Collections.Generic;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    public class MockServiceProxy : IServiceProxy
    {
        private readonly IDictionary<Type, Func<Uri, object>> _createFunctions = new Dictionary<Type, Func<Uri, object>>();

        public Type ServiceInterfaceType { get; private set; }

        public IServiceRemotingPartitionClient ServicePartitionClient { get; private set; }

        public TServiceInterface Create<TServiceInterface>(Uri serviceName) where TServiceInterface : IService
        {
            this.ServiceInterfaceType = typeof(TServiceInterface);
            return (TServiceInterface)this._createFunctions[typeof(TServiceInterface)](serviceName);
        }

        public TServiceInterface Create<TServiceInterface>(Uri serviceUri, ServicePartitionKey partitionKey = null, TargetReplicaSelector targetReplicaSelector = TargetReplicaSelector.Default, string listenerName = null) where TServiceInterface : IService
        {
            return (TServiceInterface)this._createFunctions[typeof(TServiceInterface)](serviceUri);
        }

        public void Supports<TServiceInterface>(Func<Uri, object> Create)
        {
            this._createFunctions[typeof(TServiceInterface)] = Create;
        }
    }
}