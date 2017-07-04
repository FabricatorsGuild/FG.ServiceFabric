using System;
using System.Reflection;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
	public class ServiceRequestContextWrapper : IDisposable
    {
		protected ServiceRequestContextWrapper()
        {
            //TODO: Should check for ambient context and preserve it?
            ServiceRequestContext.Current = new ServiceRequestContext();
        }

        public ServiceRequestContextWrapper(CustomServiceRequestHeader customHeader)
			:this()
        {
	        if (ServiceRequestContext.Current == null) return;

	        foreach (var header in customHeader.GetHeaders())
	        {
		        ServiceRequestContext.Current[header.Key] = header.Value;
	        }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

	    private void Dispose(bool disposing)
        {
            if (disposing)
            {
                //TODO: Should check if ambient context was used and not dispose it?
                ServiceRequestContext.Current = null;
            }
        }
    }
}