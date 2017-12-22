namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System.Collections.Immutable;

    public static class ServiceRequestContextExtensions
    {
        public static CustomServiceRequestHeader GetCustomHeader(this ServiceRequestContext context)
        {
            return new CustomServiceRequestHeader(context?.Properties ?? ImmutableDictionary<string, string>.Empty);
        }

        public static string CorrelationId(this ServiceRequestContext context)
        {
            return context[ServiceRequestContextKeys.CorrelationId];
        }

        public static string CorrelationId(this ServiceRequestContext context, string newCorrelationId)
        {
            return context[ServiceRequestContextKeys.CorrelationId] = newCorrelationId;
        }
    }
}