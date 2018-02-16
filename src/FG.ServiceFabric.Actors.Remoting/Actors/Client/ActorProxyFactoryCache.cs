using System;
using System.Collections.Concurrent;

namespace FG.ServiceFabric.Actors.Client
{
    internal class ActorProxyFactoryCache
    {
        private readonly ConcurrentDictionary<Key, Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory> proyFactories =
            new ConcurrentDictionary<Key, Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory>();

        public Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory GetFactory(ActorProxyFactory actorProxyFactory, Type serviceInterfaceType, Type actorInterfaceType)
        {
            return this.proyFactories.GetOrAdd(new Key(serviceInterfaceType, actorInterfaceType), k =>
            {
                return
                    new Microsoft.ServiceFabric.Actors.Client.ActorProxyFactory(
                        client => actorProxyFactory.CreateServiceRemotingClientFactory(client, k.ServiceInterfaceType,
                            k.ActorInterfaceType));
            });
        }

        private struct Key
        {
            private readonly int hashCode;

            public Key(Type serviceInterfaceType, Type actorInterfaceType)
            {
                this.ServiceInterfaceType = serviceInterfaceType;
                this.ActorInterfaceType = actorInterfaceType;
                this.hashCode = serviceInterfaceType?.GetHashCode() ?? 0 + actorInterfaceType?.GetHashCode() ?? 0;
            }

            public Type ActorInterfaceType { get; }

            public Type ServiceInterfaceType { get; }

            public override bool Equals(object obj)
            {
                if (obj == null || obj is Key == false)
                {
                    return false;
                }

                var other = (Key) obj;

                return other.ActorInterfaceType == this.ActorInterfaceType && other.ServiceInterfaceType == this.ServiceInterfaceType;
            }

            public override int GetHashCode()
            {
                return this.hashCode;
            }
        }
    }
}