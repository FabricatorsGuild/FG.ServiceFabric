using System;
using System.Collections.Generic;
using System.Fabric;

namespace FG.ServiceFabric.Utils
{
    public abstract class SettingsProviderBase : ISettingsProvider
    {
        private readonly ServiceContext _context;
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        protected SettingsProviderBase(ServiceContext context)
        {
            _context = context; 
        }

        protected RegistrationBuilder Configure()
        {
            return new RegistrationBuilder(this);
        }

        public class RegistrationBuilder
        {
            private readonly SettingsProviderBase _settingsProvider;
            public RegistrationBuilder(SettingsProviderBase settingsProvider) { _settingsProvider = settingsProvider; }

            public RegistrationBuilder FromSettings(string section, string key, string namedKey = null)
            {
                namedKey = namedKey ?? key;
                _settingsProvider._values.Add(namedKey, _settingsProvider.GetValueFromSettingsFile(section, key));
                return this;
            }
        }

        private string GetValueFromSettingsFile(string section, string key)
        {
            var settingsFile = _context.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings;
            if (settingsFile.Sections.Contains(section))
            {
                var configSection = settingsFile.Sections[section];
                if (configSection.Parameters.Contains(key))
                {
                    return configSection.Parameters[key].Value;
                }
            }

            throw new ArgumentException($"Key {key} not found in section {section}.");
        }

        public string this[string key]
        {
            get
            {
                _values.TryGetValue(key, out string value);
                return value;
            }
        }
    }
}