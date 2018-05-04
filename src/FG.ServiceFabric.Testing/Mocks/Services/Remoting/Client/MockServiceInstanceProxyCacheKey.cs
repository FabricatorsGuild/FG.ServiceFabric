namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    using System;

    using Microsoft.ServiceFabric.Services.Client;

    internal struct MockServiceInstanceProxyCacheKey
    {
        public MockServiceInstanceProxyCacheKey(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
        {
            this.ServiceUri = serviceUri;
            this.ServiceInterfaceType = serviceInterfaceType;
            this.PartitionKey = partitionKey;
        }

        public ServicePartitionKey PartitionKey { get; }

        public Type ServiceInterfaceType { get; }

        public Uri ServiceUri { get; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return this.Equals((MockServiceInstanceProxyCacheKey)obj);
        }

        public bool Equals(MockServiceInstanceProxyCacheKey other)
        {
            return other.PartitionKey.Value == this.PartitionKey.Value && other.ServiceInterfaceType == this.ServiceInterfaceType && other.ServiceUri == this.ServiceUri;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.ServiceUri?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (this.ServiceInterfaceType?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (this.PartitionKey?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}