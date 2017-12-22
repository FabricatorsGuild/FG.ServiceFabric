namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;
    using System.Collections.Generic;

    public class ServiceRequestContextWrapper : IDisposable
    {
        private readonly bool _shouldDispose;

        public ServiceRequestContextWrapper(CustomServiceRequestHeader customHeader)
            : this()
        {
            ServiceRequestContext.Current.Update(d => d.SetItems(customHeader.GetHeaders()));
        }

        protected ServiceRequestContextWrapper()
        {
            this._shouldDispose = true;
            ServiceRequestContext.Current.Clear();
        }

        public static ServiceRequestContextWrapper Current => new ServiceRequestContextWrapper();

        public string CorrelationId
        {
            get => ServiceRequestContext.Current.CorrelationId();
            set => ServiceRequestContext.Current.CorrelationId(value);
        }

        public IEnumerable<string> Keys => ServiceRequestContext.Current.Keys;

        public string RequestUri
        {
            get => ServiceRequestContext.Current[ServiceRequestContextKeys.RequestUri];
            set => ServiceRequestContext.Current[ServiceRequestContextKeys.RequestUri] = value;
        }

        public string UserId
        {
            get => ServiceRequestContext.Current[ServiceRequestContextKeys.UserId];
            set => ServiceRequestContext.Current[ServiceRequestContextKeys.UserId] = value;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

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