using System.Collections.Generic;

namespace FG.ServiceFabric.Testing.Setup
{
    internal class ApplicationManifestServiceManifestImport
    {
        public string Name { get; set; }
        public string Version { get; set; }

        public ConfigSection[] Sections { get; set; }

        public class ConfigOverride
        {
            public string Name { get; set; }

            public ConfigOverrideSettings Settings { get; set; }
        }

        public class ConfigOverrideSettings
        {
            public ConfigSection[] Sections { get; set; }
        }

        public class ConfigSection
        {
            public string Name { get; set; }
            public KeyValuePair<string, string>[] Parameters { get; set; }
        }
    }
}