namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ServiceFabric.Services.Client;

    internal class BaseMockActorServiceInstance : MockServiceInstance
    {
        public MockActorContainer ActorContainer { get; protected set; } = new MockActorContainer();

        public override bool Equals(Type actorInterfaceType, ServicePartitionKey partitionKey)
        {
            if (this.ActorRegistration?.ServiceRegistration.ServiceDefinition.PartitionKind != partitionKey.Kind)
            {
                return false;
            }

            var partitionId = this.ActorRegistration?.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

            return this.ActorRegistration.InterfaceType == actorInterfaceType && this.Partition.PartitionInformation.Id == partitionId;
        }

        public override bool Equals(Uri serviceUri, Type serviceInterfaceType, ServicePartitionKey partitionKey)
        {
            if (this.ActorRegistration.ServiceRegistration.ServiceDefinition.PartitionKind != partitionKey.Kind)
            {
                return false;
            }

            var partitionId = this.ActorRegistration.ServiceRegistration.ServiceDefinition.GetPartion(partitionKey);

            return serviceUri.ToString().Equals(this.ServiceUri.ToString(), StringComparison.InvariantCultureIgnoreCase)
                   && this.ActorRegistration.ServiceRegistration.InterfaceTypes.Any(i => i == serviceInterfaceType)
                   && this.Partition.PartitionInformation.Id == partitionId;
        }

        public override string ToString()
        {
            return $"{nameof(BaseMockActorServiceInstance)}: {this.ServiceUri}";
        }
    }
}