using System.Collections.Generic;

namespace FG.ServiceFabric.Testing.Setup
{
	public class ServiceConfigSection : IServiceConfigSection
	{
		public string Name { get; set; }
		public IDictionary<string, string> Parameters { get; set; }
	}
}