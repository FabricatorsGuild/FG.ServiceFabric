namespace FG.ServiceFabric.Testing.Mocks.Actors.Client
{
    using System;

    using FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client;

    public class SimpleMockableActorRegistration : BaseMockableActorRegistration
    {
        public SimpleMockableActorRegistration(IMockableServiceRegistration serviceRegistration, SimpleActorRegistrationConfiguration simpleActorConfiguration, Type interfaceType)
        {
            this.SimpleActorConfiguration = simpleActorConfiguration;
            this.ServiceRegistration = serviceRegistration;
            this.InterfaceType = interfaceType;
        }

        public override bool IsSimple => true;
    }
}