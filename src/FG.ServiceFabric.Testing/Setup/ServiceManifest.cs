using System.Linq;
using System.Xml.Linq;

namespace FG.ServiceFabric.Testing.Setup
{
    internal class ServiceManifest : IServiceManifest
    {
        private ServiceManifest()
        {
        }

        internal ServiceManifest(string name, string version, string[] serviceTypes)
        {
        }

        public string Name { get; set; }
        public string Version { get; set; }
        public string[] ServiceTypes { get; set; }

        public static IServiceManifest Load(string serviceManifestPath)
        {
            var xdoc = XDocument.Load(serviceManifestPath);
            var xns = (XNamespace) "http://schemas.microsoft.com/2011/01/fabric";

            var name = xdoc.Element(xns + "ServiceManifest")?.Attribute("Name")?.Value;
            var version = xdoc.Element(xns + "ServiceManifest")?.Attribute("Version")?.Value;

            var statelessServiceTypeElements =
                xdoc.Element(xns + "ServiceManifest")?.Element(xns + "ServiceTypes")
                    ?.Elements(xns + "StatelessServiceType")
                    ?.Select(e => e.Attribute("ServiceTypeName")?.Value).Where(e => e != null) ?? new string[0];
            var statefulServiceTypeElements =
                xdoc.Element(xns + "ServiceManifest")?.Element(xns + "ServiceTypes")
                    ?.Elements(xns + "StatefulServiceType")
                    ?.Select(e => e.Attribute("ServiceTypeName")?.Value).Where(e => e != null) ?? new string[0];

            var serviceManifext = new ServiceManifest
            {
                Name = name,
                Version = version,
                ServiceTypes = statefulServiceTypeElements.Union(statelessServiceTypeElements).ToArray()
            };

            return serviceManifext;
        }

        public static IServiceManifest DefaultFor(string serviceName)
        {
            return new ServiceManifest($"{serviceName}Pkg", "1.0.0", new[] {serviceName});
        }
    }

    public static class ServiceManifestExtensions
    {
        public static IServiceManifest OrDefaultFor(this IServiceManifest that, string serviceName)
        {
            if (that != null) return that;

            return ServiceManifest.DefaultFor(serviceName);
        }
    }
}