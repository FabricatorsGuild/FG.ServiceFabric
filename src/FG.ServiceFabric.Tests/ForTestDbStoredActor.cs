using FG.ServiceFabric.Actors.Runtime;
using FG.ServiceFabric.DocumentDb.Testing;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.DbStoredActor;

namespace FG.ServiceFabric.Tests
{
    public class ForTestDbStoredActor
    {
        public static void Setup(MockFabricRuntime mockFabricRuntime, InMemoryStateSession inMemoryStateSession)
        {
            mockFabricRuntime.SetupActor(
                (service, id) =>
                    new DbStoredActor.DbStoredActor(
                        actorService: service,
                        actorId: id),
                (context, actorTypeInformation, stateProvider, stateManagerFactory) =>
                    new DbStoredActorService(
                        context: context,
                        actorTypeInfo: actorTypeInformation,
                        stateProvider: new DocumentDbActorStateProvider(inMemoryStateSession, context, actorTypeInformation, stateProvider),
                        stateManagerFactory: stateManagerFactory)
                ,
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}