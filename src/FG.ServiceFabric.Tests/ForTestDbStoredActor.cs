using FG.ServiceFabric.DocumentDb.Testing;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;

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
                        actorId: id,
                        stateWriterFactory: () => inMemoryStateSession),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}