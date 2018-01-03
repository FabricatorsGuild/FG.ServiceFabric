namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Runtime.Remoting.Messaging;

    using FG.Common.CallContext;

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