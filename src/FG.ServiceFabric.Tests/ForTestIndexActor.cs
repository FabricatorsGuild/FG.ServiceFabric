using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Tests.EventStoredActor;

namespace FG.ServiceFabric.Tests
{
    public class ForTestIndexActor
    {
        public static void Setup(MockFabricRuntime mockFabricRuntime)
        {
            mockFabricRuntime.SetupActor<IndexActor>((service, id) =>
                    new IndexActor(
                        actorService: service,
                        actorId: id),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}