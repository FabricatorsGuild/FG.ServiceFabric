using System;
using FG.CQRS;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.PersonActor;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.CQRS
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
            SetupPersonActor(FabricRuntime);
            SetupPersonIndexActor(FabricRuntime);
        }

        public static void SetupPersonActor(MockFabricRuntime mockFabricRuntime)
        {
            mockFabricRuntime.SetupActor(
                (service, id) =>
                    new PersonActor.PersonActor(
                        actorService: service,
                        actorId: id), 
                (context, actorTypeInformation, stateProvider, stateManagerFactory) =>
                    new PersonActorService(
                        context: context,
                        actorTypeInfo: actorTypeInformation,
                        stateProvider: stateProvider,
                        stateManagerFactory: stateManagerFactory),
                createStateProvider: () => new MockActorStateProvider(mockFabricRuntime));
        }

        public static void SetupPersonIndexActor(MockFabricRuntime mockFabricRuntime)
        {
            mockFabricRuntime.SetupActor<PersonIndexActor>((service, id) =>
                   new PersonActor.PersonIndexActor(
                       actorService: service,
                       actorId: id));

        }
    }
}
