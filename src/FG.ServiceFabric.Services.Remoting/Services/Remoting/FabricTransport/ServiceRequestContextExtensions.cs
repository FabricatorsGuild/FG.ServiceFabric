using System.Collections.Generic;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
	public static class ServiceRequestContextExtensions
	{
		public static CustomServiceRequestHeader GetCustomHeader(
			this ServiceRequestContext context)
		{			
			var headerValues = new Dictionary<string, string>();
			if (context != null)
			{


				var contextWrapper = ServiceRequestContextWrapper.Current;
				foreach (var key in contextWrapper.GetAllKeys())
				{
					headerValues[key ] = context[key];
				}
			}

			var customServiceRequestHeader = new CustomServiceRequestHeader(headerValues);
			return customServiceRequestHeader;
		}
	}
}