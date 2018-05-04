namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    using System;
    using System.Collections.Concurrent;

    internal class MockServiceInstanceProxyCache
    {
        private readonly ConcurrentDictionary<MockServiceInstanceProxyCacheKey, object> instanceProxies = new ConcurrentDictionary<MockServiceInstanceProxyCacheKey, object>();

        public object GetOrAdd(MockServiceInstanceProxyCacheKey key, Func<MockServiceInstanceProxyCacheKey, object> createFunc)
        {
            return this.instanceProxies.GetOrAdd(key, createFunc(key));
        }
    }
}