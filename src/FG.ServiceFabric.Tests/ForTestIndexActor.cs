using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor;

namespace FG.ServiceFabric.Tests
{
    public class ForTestIndexActor
    {
        public static void Setup(MockFabricApplication mockFabricApplication)
        {
            mockFabricApplication.SetupActor<IndexActor>(
				(service, id) =>
                    new IndexActor(
                        actorService: service,
                        actorId: id),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}