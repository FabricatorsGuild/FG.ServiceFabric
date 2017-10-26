using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using FG.ServiceFabric.Tests.EventStoredActor;

namespace FG.ServiceFabric.Tests
{
    public class ForTestIndexActor
    {
        public static void Setup(MockFabricRuntime mockFabricRuntime, string applicationName)
        {
            mockFabricRuntime.SetupActor<IndexActor>(
	            applicationName,
				(service, id) =>
                    new IndexActor(
                        actorService: service,
                        actorId: id),
                serviceDefinition: MockServiceDefinition.CreateUniformInt64Partitions(1));
        }
    }
}