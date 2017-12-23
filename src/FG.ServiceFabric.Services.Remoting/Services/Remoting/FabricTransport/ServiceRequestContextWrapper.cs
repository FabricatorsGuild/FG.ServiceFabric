namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    /// <summary>
    ///     Provides a scope for the current service context
    /// </summary>
    public class ServiceRequestContextWrapper : IDisposable
    {
        private readonly bool _shouldDispose;

        private readonly ImmutableDictionary<string, string> previousProperties;

        private readonly ServiceRequestContext serviceRequestContext;

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
        public ServiceRequestContextWrapper(CustomServiceRequestHeader customHeader, ServiceRequestContext serviceRequestContext)
            : this()
        {
            this.serviceRequestContext = serviceRequestContext;
            ServiceRequestContext.Current.Update(customHeader.GetHeaders(), (headers, d) => d.SetItems(headers));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceRequestContextWrapper" /> class.
        /// </summary>
        /// <param name="serviceRequestContext">The service request context to use</param>
        public ServiceRequestContextWrapper(ServiceRequestContext serviceRequestContext)
            : this()
        {
            this.serviceRequestContext = serviceRequestContext;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceRequestContextWrapper" /> class.
        /// </summary>
        protected ServiceRequestContextWrapper()
        {
            this._shouldDispose = true;
            this.previousProperties = ServiceRequestContext.Current.InternalProperties;
        }

        /// <summary>
        /// Gets a new service request context scope
        /// </summary>
        public static ServiceRequestContextWrapper Current => new ServiceRequestContextWrapper();

        /// <summary>
        /// Gets or sets the correlation id
        /// </summary>
        public string CorrelationId
        {
            get => this.serviceRequestContext.CorrelationId();
            set => this.serviceRequestContext.CorrelationId(value);
        }

        /// <summary>
        ///  Gets all property names/keys
        /// </summary>
        public IEnumerable<string> Keys => this.serviceRequestContext.Keys;

        /// <summary>
        /// Gets or sets the request id
        /// </summary>
        public string RequestUri
        {
            get => this.serviceRequestContext[ServiceRequestContextKeys.RequestUri];
            set => this.serviceRequestContext[ServiceRequestContextKeys.RequestUri] = value;
        }

        /// <summary>
        /// Gets or sets the user id
        /// </summary>
        public string UserId
        {
            get => ServiceRequestContext.Current[ServiceRequestContextKeys.UserId];
            set => ServiceRequestContext.Current[ServiceRequestContextKeys.UserId] = value;
        }

        /// <summary>
        /// Disposes the instance
        /// </summary>
        public void Dispose()
        {
            this.serviceRequestContext.InternalProperties = this.previousProperties;
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets all property names/keys
        /// </summary>
        /// <returns>The property names/keys</returns>
        public IEnumerable<string> GetAllKeys()
        {
            return ServiceRequestContext.Current.Keys;
        }

        private void Dispose(bool disposing)
        {
            if (disposing && this._shouldDispose)
            {
                ServiceRequestContext.Current.Clear();
            }
        }
    }
}