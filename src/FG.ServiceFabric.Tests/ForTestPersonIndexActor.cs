using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.PersonActor;

namespace FG.ServiceFabric.Tests
{
    public class ForTestPersonIndexActor
    {
        public static void Setup(MockFabricRuntime mockFabricRuntime)
        {
            mockFabricRuntime.SetupActor<PersonIndexActor>((service, id) =>
                    new PersonActor.PersonIndexActor(
                        actorService: service,
                        actorId: id),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}