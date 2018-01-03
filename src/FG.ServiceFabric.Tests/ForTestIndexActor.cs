using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor;

namespace FG.ServiceFabric.Tests
{
    public class ForTestIndexActor
    {
        public static void Setup(MockFabricApplication mockFabricApplication)
        {
            mockFabricApplication.SetupActor(
                (service, id) =>
                    new IndexActor(
                        service,
                        id),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}