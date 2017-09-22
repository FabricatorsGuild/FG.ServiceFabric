using System;
using System.Collections.Generic;
using System.Reflection;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
	public class ServiceRequestContextWrapper : IDisposable
	{
		private static class ServiceRequestContextKeys
		{
			public const string CorrelationId = "correlationId";
			public const string UserId = "userId";
			public const string RequestUri = "requestUri";
		}

		private readonly bool _shouldDispose = false;
		protected ServiceRequestContextWrapper()
        {
	        if (ServiceRequestContext.Current == null)
	        {
		        _shouldDispose = true;
				ServiceRequestContext.Current = new ServiceRequestContext();
			}
        }

		public static ServiceRequestContextWrapper Current => new ServiceRequestContextWrapper();

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
	            if (_shouldDispose)
	            {
		            ServiceRequestContext.Current = null;
	            }
            }
        }

		public string CorrelationId
		{
			get { return ServiceRequestContext.Current?[ServiceRequestContextKeys.CorrelationId]; }
			set
			{
				if (ServiceRequestContext.Current != null)
				{
					ServiceRequestContext.Current[ServiceRequestContextKeys.CorrelationId] = value;
				}
			}
		}
		public string UserId
		{
			get { return ServiceRequestContext.Current?[ServiceRequestContextKeys.UserId]; }
			set
			{
				if (ServiceRequestContext.Current != null)
				{
					ServiceRequestContext.Current[ServiceRequestContextKeys.UserId] = value;
				}
			}
		}
		public string RequestUri
		{
			get { return ServiceRequestContext.Current?[ServiceRequestContextKeys.RequestUri]; }
			set
			{
				if (ServiceRequestContext.Current != null)
				{
					ServiceRequestContext.Current[ServiceRequestContextKeys.RequestUri] = value;
				}
			}
		}

		public IEnumerable<string> GetAllKeys()
		{
			return ServiceRequestContext.Current?.Keys ?? new string[0];
		}
	}
}