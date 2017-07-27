using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.PersonActor;

namespace FG.ServiceFabric.Tests
{
    public class ForTestPersonActor
    {
        public static void Setup(MockFabricRuntime mockFabricRuntime)
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
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}