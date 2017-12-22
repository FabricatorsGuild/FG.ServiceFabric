namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System.Collections.Generic;
    using System.Linq;

    public static class CustomServiceRequestHeaderExtensions
    {
        public static CustomServiceRequestHeader GetCustomHeader(this IEnumerable<ServiceRequestHeader> headers)
        {
            return (CustomServiceRequestHeader)headers.FirstOrDefault(h => h is CustomServiceRequestHeader);
        }
    }
}