using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FG.ServiceFabric.Testing.Setup
{
	internal class ServiceConfig : IServiceConfig
	{

		public IServiceConfigSection[] Sections { get; set; }

		public static IServiceConfig Load(string serviceConfigPath)
		{
			var xdoc = XDocument.Load(serviceConfigPath);
			var xns = (XNamespace)"http://schemas.microsoft.com/2011/01/fabric";

			var sectionElements = xdoc.Element(xns + "Settings")?.Elements(xns + "Section");

			var sections = new List<ServiceConfigSection>();
			foreach (var sectionElement in sectionElements)
			{
				var name = sectionElement.Attribute("Name")?.Value;
				var parameters = sectionElement.Elements(xns + "Parameter")
					.Select((parameterElement, i) => new {
						Name = parameterElement.Attribute("Name")?.Value ?? $"parameter{i}",
						Value = parameterElement.Attribute("Value")?.Value ?? ""})
					.ToDictionary(p => p.Name, p => p.Value);

				var section = new ServiceConfigSection() { Name = name, Parameters = parameters };
				sections.Add(section);
			}

			var serviceConfig = new ServiceConfig()
			{
				Sections = sections.ToArray()
			};

			return serviceConfig;
		}

		public static IServiceConfig Default()
		{
			return new ServiceConfig()
			{
				Sections = new IServiceConfigSection[]
				{
					new ServiceConfigSection() {Name = "Config", Parameters = new Dictionary<string, string>()},
				}
			};
		}
	}

	public static class ServiceConfigExtensions
	{
		public static IServiceConfig OrDefault(this IServiceConfig that)
		{
			if (that != null) return that;

			return ServiceConfig.Default();
		}
	}
}