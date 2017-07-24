using System;
using FG.ServiceFabric.DocumentDb.Testing;
using FG.ServiceFabric.Services.Runtime;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.DbStoredActor;
using FG.ServiceFabric.Tests.PersonActor;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using NUnit.Framework;

namespace FG.ServiceFabric.Tests.Persistance
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
            SetupDbStoredActor(FabricRuntime, new InMemoryStateSession());
        }

        public static void SetupDbStoredActor(MockFabricRuntime mockFabricRuntime, InMemoryStateSession inMemoryStateSession)
        {
            mockFabricRuntime.SetupActor(
                (service, id) =>
                    new DbStoredActor.DbStoredActor(
                        actorService: service,
                        actorId: id,
                        stateWriterFactory: () => inMemoryStateSession
                        )
                //, 
                //(context, actorTypeInformation, stateProvider, stateManagerFactory) =>
                //    new DbStoredActorService(
                //        context: context,
                //        actorTypeInfo: actorTypeInformation,
                //        stateProvider: stateProvider,
                //        stateManagerFactory: stateManagerFactory),
                //createStateProvider: () => new MockActorStateProvider(mockFabricRuntime)
                );
        }
    }
}
