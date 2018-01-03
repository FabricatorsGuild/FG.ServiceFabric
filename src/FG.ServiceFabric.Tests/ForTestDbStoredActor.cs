using FG.ServiceFabric.DocumentDb.Testing;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;

namespace FG.ServiceFabric.Tests
{
    public class ForTestDbStoredActor
    {
        public static void Setup(MockFabricApplication mockFabricApplication, InMemoryStateSession inMemoryStateSession)
        {
            mockFabricApplication.SetupActor(
                (service, id) =>
                    new DbStoredActor.DbStoredActor(
                        service,
                        id,
                        () => inMemoryStateSession),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}