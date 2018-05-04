namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    using System;
    using System.Collections.Concurrent;

    internal class MockActorProxyFactories
    {
        public static ConcurrentDictionary<Type, Delegate> Factories { get; } = new ConcurrentDictionary<Type, Delegate>();


    }
}