using FG.Common.CallContext;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    /// <summary>
    ///     Provides a scope for the current service context
    /// </summary>
    public class ServiceRequestContextWrapper : BaseCallContextWrapper<ServiceRequestContext, string>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceRequestContextWrapper" /> class.
        /// </summary>
        /// <param name="customHeader">
        ///     The custom service request header
        /// </param>
        public ServiceRequestContextWrapper(CustomServiceRequestHeader customHeader)
            : this(customHeader, ServiceRequestContext.Current)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceRequestContextWrapper" /> class.
        /// </summary>
        /// <param name="customHeader">
        ///     The custom service request header
        /// </param>
        /// <param name="serviceRequestContext">The service request context to use</param>
        public ServiceRequestContextWrapper(CustomServiceRequestHeader customHeader,
            ServiceRequestContext serviceRequestContext)
            : base(serviceRequestContext)
        {
            ServiceRequestContext.Current.Update(customHeader.GetHeaders(), (headers, d) => d.SetItems(headers));
            ShouldDispose = true;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceRequestContextWrapper" /> class.
        /// </summary>
        /// <param name="serviceRequestContext">The service request context to use</param>
        public ServiceRequestContextWrapper(ServiceRequestContext serviceRequestContext)
            : base(serviceRequestContext ?? ServiceRequestContext.Current)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceRequestContextWrapper" /> class.
        /// </summary>
        protected ServiceRequestContextWrapper() : base(ServiceRequestContext.Current,
            ServiceRequestContext.Current.Properties)
        {
            ShouldDispose = true;
        }

        /// <summary>
        ///     Gets a new service request context scope
        /// </summary>
        public static ServiceRequestContextWrapper Current => new ServiceRequestContextWrapper();

        /// <summary>
        ///     Gets or sets the correlation id
        /// </summary>
        public string CorrelationId
        {
            get => Context.CorrelationId();
            set => Context.CorrelationId(value);
        }

        /// <summary>
        ///     Gets or sets the request id
        /// </summary>
        public string RequestUri
        {
            get => Context[ServiceRequestContextKeys.RequestUri];
            set => Context[ServiceRequestContextKeys.RequestUri] = value;
        }

        /// <summary>
        ///     Gets or sets the user id
        /// </summary>
        public string UserId
        {
            get => Context[ServiceRequestContextKeys.UserId];
            set => Context[ServiceRequestContextKeys.UserId] = value;
        }
    }
}