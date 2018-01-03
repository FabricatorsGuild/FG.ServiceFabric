using System.Collections.Generic;

namespace FG.ServiceFabric.Testing.Setup
{
    public interface IServiceConfigSection
    {
        string Name { get; set; }
        IDictionary<string, string> Parameters { get; set; }
    }
}