using System.Collections.Immutable;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    /// <summary>
    ///     Provides extensions for <see cref="ServiceRequestContext" />
    /// </summary>
    public static class ServiceRequestContextExtensions
    {
        /// <summary>
        ///     Gets an instance of <see cref="CustomServiceRequestHeader" />, populated with the current context properties
        /// </summary>
        /// <param name="context">A <see cref="ServiceRequestContext" /></param>
        /// <returns>A <see cref="CustomServiceRequestHeader" /></returns>
        public static CustomServiceRequestHeader GetCustomHeader(this ServiceRequestContext context)
        {
            return new CustomServiceRequestHeader(context?.Properties ?? ImmutableDictionary<string, string>.Empty);
        }

        /// <summary>
        ///     Gets the correlation id
        /// </summary>
        /// <param name="context">A <see cref="ServiceRequestContext" /></param>
        /// <returns>The correlation id, or null if no correlation id is available</returns>
        public static string CorrelationId(this ServiceRequestContext context)
        {
            return context[ServiceRequestContextKeys.CorrelationId];
        }

        /// <summary>
        ///     Sets the correlation id
        /// </summary>
        /// <param name="context">A <see cref="ServiceRequestContext" /></param>
        /// <param name="newCorrelationId">The new correlation id</param>
        /// <returns>The <see cref="ServiceRequestContext" /></returns>
        public static ServiceRequestContext CorrelationId(this ServiceRequestContext context, string newCorrelationId)
        {
            context[ServiceRequestContextKeys.CorrelationId] = newCorrelationId;
            return context;
        }

        /// <summary>
        ///     Starts a new service request scope, restoring the service request context when the scope is disposed.
        /// </summary>
        /// <param name="serviceRequestContext">The service request context</param>
        /// <param name="customHeader">The custom header</param>
        /// <returns>A service request context wrapper <see cref="ServiceRequestContextWrapper" /></returns>
        public static ServiceRequestContextWrapper BeginScope(this ServiceRequestContext serviceRequestContext,
            CustomServiceRequestHeader customHeader)
        {
            return new ServiceRequestContextWrapper(serviceRequestContext);
        }
    }
}