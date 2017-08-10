using System;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.CQRS;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests
{
    public abstract class TestBase
    {
        protected MockFabricRuntime FabricRuntime;

        protected IServiceProxyFactory ServiceProxyFactory => FabricRuntime.ServiceProxyFactory;
        protected IActorProxyFactory ActorProxyFactory => FabricRuntime.ActorProxyFactory;
        protected ApplicationUriBuilder ApplicationUriBuilder => FabricRuntime.ApplicationUriBuilder;
        
        [SetUp]
        public void Setup()
        {
            FabricRuntime = new MockFabricRuntime(Guid.NewGuid().ToString());
            // ReSharper disable once VirtualMemberCallInConstructor
            SetupRuntime();
        }

        protected virtual void SetupRuntime()
        {
        }
    }
}
