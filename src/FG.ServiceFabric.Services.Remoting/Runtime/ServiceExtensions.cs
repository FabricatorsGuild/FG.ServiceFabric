using System.Fabric;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Services.Runtime
{
    public static class ServiceExtensions
    {
        public static ServiceContext GetServiceContext(this IService service)
        {
            var statefulService = service as StatefulService;
            if (statefulService != null)
                return statefulService.Context;

            var statelessService = service as StatelessService;
            if (statelessService != null)
                return statelessService.Context;

            return null;
        }
    }
}