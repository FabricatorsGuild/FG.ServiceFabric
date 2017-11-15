using System;
using System.Security.Cryptography.X509Certificates;

namespace FG.ServiceFabric.Services.Runtime.StateSession.Metadata
{
	public interface IServiceMetadata
	{
		string ServiceName { get; set; }
		string PartitionKey { get; set; }
	}

	public class ServiceMetadata : IServiceMetadata
	{
		public string ServiceName { get; set; }
		public string PartitionKey { get; set; }
	}
}