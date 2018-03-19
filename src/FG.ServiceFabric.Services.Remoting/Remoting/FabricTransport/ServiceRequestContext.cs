using FG.Common.CallContext;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    /// <summary>
    ///     Provides a handler and a container for managing the current logical call context values
    /// </summary>
    public sealed class ServiceRequestContext : BaseCallContext<ServiceRequestContext, string>
    {
        private ServiceRequestContext()
        {
        }

        /// <summary>
        ///     Gets the current service request context
        /// </summary>
        public static ServiceRequestContext Current { get; } = new ServiceRequestContext();
    }
}