using System.IO;
using System.Linq;
using System.Reflection;

namespace FG.ServiceFabric.Testing.Setup
{
    internal class ServiceProjectAssemblyResolver
    {
        public ServiceProject ServiceProject { get; set; }

        public Assembly Resolve(string reference)
        {
            if (ServiceProject == null) return null;

            var referenceName = reference.Split(',')[0];
            var assemblyFile = Directory
                .GetFiles(ServiceProject.OutputPath)
                .FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == referenceName);
            if (assemblyFile != null)
                return Assembly.LoadFrom(assemblyFile);
            return null;
        }
    }
}