using System;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public class ServiceRequestContextWrapper : IDisposable
    {
        public ServiceRequestContextWrapper()
        {
            //TODO: Should check for ambient context and preserve it?
            ServiceRequestContext.Current = new ServiceRequestContext();
        }

        public ServiceRequestContextWrapper(CustomServiceRequestHeader customHeader)
        {
            //TODO: Should check for ambient context and preserve it?
            ServiceRequestContext.Current = new ServiceRequestContext();

            this.CorrelationId = customHeader[ServiceRequestContextKeys.CorrelationId] ?? Guid.NewGuid().ToString();
            this.UserId = customHeader[ServiceRequestContextKeys.UserId];
        }

        public string CorrelationId
        {
            get
            {
                return ServiceRequestContext.Current?[ServiceRequestContextKeys.CorrelationId];
            }
            set {
                if (ServiceRequestContext.Current != null)
                {
                    ServiceRequestContext.Current[ServiceRequestContextKeys.CorrelationId] = value;
                }
            }
        }

        public Uri RequestUri
        {
            get
            {
                var requestUriString = ServiceRequestContext.Current?[ServiceRequestContextKeys.RequestUri];
                if (requestUriString != null)
                {
                    return  new Uri(requestUriString);
                }
                return null;
            }
            set
            {
                if (ServiceRequestContext.Current != null)
                {
                    ServiceRequestContext.Current[ServiceRequestContextKeys.RequestUri] = value?.ToString();
                }
            }
        }

        public string UserId
        {
            get
            {
                return ServiceRequestContext.Current?[ServiceRequestContextKeys.UserId];
            }
            set
            {
                if (ServiceRequestContext.Current != null)
                {
                    ServiceRequestContext.Current[ServiceRequestContextKeys.UserId] = value;
                }
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //TODO: Should check if ambient context was used and not dispose it?
                ServiceRequestContext.Current = null;
            }
        }
    }
}