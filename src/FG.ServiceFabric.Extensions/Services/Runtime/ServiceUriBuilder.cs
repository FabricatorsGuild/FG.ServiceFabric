using System;
using System.Fabric;

namespace FG.ServiceFabric.Services.Runtime
{
	public class ServiceUriBuilder
	{
		public ServiceUriBuilder(string serviceInstance)
		{
			this.ActivationContext = FabricRuntime.GetActivationContext();
			this.ServiceInstance = serviceInstance;
		}

		public ServiceUriBuilder(ICodePackageActivationContext context, string serviceInstance)
		{
			this.ActivationContext = context;
			this.ServiceInstance = serviceInstance;
		}

		public ServiceUriBuilder(ICodePackageActivationContext context, string applicationInstance, string serviceInstance)
		{
			this.ActivationContext = context;
			this.ApplicationInstance = applicationInstance;
			this.ServiceInstance = serviceInstance;
		}

		/// <summary>
		///     The name of the application instance that contains he service.
		/// </summary>
		public string ApplicationInstance { get; private set; }

		/// <summary>
		///     The name of the service instance.
		/// </summary>
		public string ServiceInstance { get; private set; }

		/// <summary>
		///     The local activation context
		/// </summary>
		public ICodePackageActivationContext ActivationContext { get; private set; }

		/// <summary>
		///     Builds a Uri to the specified Service Instance
		/// </summary>
		/// <returns></returns>
		public Uri ToUri()
		{
			var applicationInstance = this.ApplicationInstance ?? this.ActivationContext.ApplicationName;
			applicationInstance = this.ActivationContext.ApplicationName.Replace("fabric:/", String.Empty);
			return new Uri("fabric:/" + applicationInstance + "/" + this.ServiceInstance);
		}

		public static implicit operator Uri(ServiceUriBuilder serviceUriBuilder)
		{
			return serviceUriBuilder.ToUri();
		}
	}
}