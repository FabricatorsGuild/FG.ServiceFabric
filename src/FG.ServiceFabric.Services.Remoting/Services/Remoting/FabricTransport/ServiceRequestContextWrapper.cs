namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Generic;

    public class ServiceRequestContextWrapper : MarshalByRefObject, IDisposable
    {
        private readonly bool _shouldDispose;

        public ServiceRequestContextWrapper(CustomServiceRequestHeader customHeader)
            : this()
        {
            if (ServiceRequestContext.Current == null)
            {
                return;
            }

            foreach (var header in customHeader.GetHeaders())
            {
                ServiceRequestContext.Current[header.Key] = header.Value;
            }
        }

        protected ServiceRequestContextWrapper()
        {
            if (ServiceRequestContext.Current == null)
            {
                this._shouldDispose = true;
                ServiceRequestContext.Current = new ServiceRequestContext();
            }
        }

        public static ServiceRequestContextWrapper Current => new ServiceRequestContextWrapper();

        public string CorrelationId
        {
            get => ServiceRequestContext.Current?[ServiceRequestContextKeys.CorrelationId];
            set
            {
                if (ServiceRequestContext.Current != null)
                {
                    ServiceRequestContext.Current[ServiceRequestContextKeys.CorrelationId] = value;
                }
            }
        }

        public string RequestUri
        {
            get => ServiceRequestContext.Current?[ServiceRequestContextKeys.RequestUri];
            set
            {
                if (ServiceRequestContext.Current != null)
                {
                    ServiceRequestContext.Current[ServiceRequestContextKeys.RequestUri] = value;
                }
            }
        }

        public string UserId
        {
            get => ServiceRequestContext.Current?[ServiceRequestContextKeys.UserId];
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
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return ServiceRequestContext.Current?.Keys ?? new string[0];
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._shouldDispose)
                {
                    ServiceRequestContext.Current = null;
                }
            }
        }

        private static class ServiceRequestContextKeys
        {
            public const string CorrelationId = "correlationId";

            public const string RequestUri = "requestUri";

            public const string UserId = "userId";
        }
    }
}