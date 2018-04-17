namespace FG.ServiceFabric.Testing.Mocks.Services.Runtime
{
    internal class SimpleMockActorServiceInstance : BaseMockActorServiceInstance
    {
        public SimpleMockActorServiceInstance()
        {
            // Create a new actor service passing a parameter to the current actor container
            this.ServiceInstance = this.ActorRegistration.SimpleActorConfiguration.ActorServiceFactory(this.ActorContainer);
        }

        public override string ToString()
        {
            return $"{nameof(SimpleMockActorServiceInstance)}: {this.ServiceUri}";
        }
    }
}