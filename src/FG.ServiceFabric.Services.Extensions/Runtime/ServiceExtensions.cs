using System.Fabric;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Services.Runtime
{
    public static class ServiceExtensions
    {
        public static ServiceContext GetServiceContext(this IService service)
        {
            var statefulService = service as Microsoft.ServiceFabric.Services.Runtime.StatefulService;
            if (statefulService != null)
                return statefulService.Context;

            var statelessService = service as Microsoft.ServiceFabric.Services.Runtime.StatelessService;
            if (statelessService != null)
                return statelessService.Context;

            return null;
        }
    }
}