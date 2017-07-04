using System.Collections.Generic;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
	public static class ServiceRequestContextExtensions
	{
		public static CustomServiceRequestHeader GetCustomHeader(
			this ServiceRequestContext context, 
			IServiceRequestContextWrapperHandler keyHandler = null)
		{			
			var headerValues = new Dictionary<string, string>();
			if (context != null)
			{
				foreach (var key in (keyHandler ?? new DefaultServiceRequestContextWrapperHandler()).GetKeys())
				{
					headerValues[key ] = context[key];
				}
			}

			var customServiceRequestHeader = new CustomServiceRequestHeader(headerValues);
			return customServiceRequestHeader;
		}
	}
}