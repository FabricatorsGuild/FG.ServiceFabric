using System;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Services.Runtime
{
	public class ServiceReference
	{
		public IService Service { get; set; }
		public Uri ServiceUri { get; set; }
	}
}