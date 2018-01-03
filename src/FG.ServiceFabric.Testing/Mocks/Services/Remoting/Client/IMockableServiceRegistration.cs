using System;
using System.Fabric;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    public interface IMockableServiceRegistration
    {
        bool IsStateful { get; }
        Type[] InterfaceTypes { get; }
        Type ImplementationType { get; }
        CreateStatefulService CreateStatefulService { get; }
        CreateStatelessService CreateStatelessService { get; }
        CreateStateManager CreateStateManager { get; }
        MockServiceDefinition ServiceDefinition { get; set; }
        Uri ServiceUri { get; set; }
        string Name { get; set; }
    }

    public static class MockableServiceRegistrationExtensions
    {
        public static string GetApplicationName(this IMockableServiceRegistration serviceRegistration)
        {
            var uri = serviceRegistration.ServiceUri.AbsoluteUri;
            if (uri.Matches(@"fabric:/[^/]*/.*", StringComparison.InvariantCulture, false))
            {
                var applicationName = uri.Split('/')[1];
                return applicationName;
            }
            return uri.RemoveFromEnd(serviceRegistration.Name).RemoveFromEnd("/").RemoveFromStart("fabric:/");
        }
    }

    public delegate IReliableStateManagerReplica2 CreateStateManager();

    public delegate StatefulServiceBase CreateStatefulService(
        StatefulServiceContext context,
        IReliableStateManagerReplica2 stateManager);

    public delegate TService CreateStatefulService<out TService>(
        StatefulServiceContext context,
        IReliableStateManagerReplica2 stateManager)
        where TService : IService;

    public delegate StatelessService CreateStatelessService(
        StatelessServiceContext context);


    public delegate TService CreateStatelessService<out TService>(
        StatelessServiceContext context)
        where TService : IService;
}