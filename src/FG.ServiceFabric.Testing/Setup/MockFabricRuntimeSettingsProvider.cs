using System.Fabric;
using System.Linq;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Utils;

namespace FG.ServiceFabric.Testing.Setup
{
    public class MockFabricRuntimeSettingsProvider : SettingsProviderBase
    {
        public MockFabricRuntimeSettingsProvider(ServiceContext context) : base(context)
        {
            var configurationPackageNames = context.CodePackageActivationContext.GetConfigurationPackageNames();
            if (!configurationPackageNames.Contains("Config"))
                throw new MockFabricSetupException(
                    $"Expected {context.ServiceName} CodePackageActivationContext to contain a ConfigurationPackage for 'config'");

            var configurationPackageObject =
                context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            Configure()
                .FromSettings(configurationPackageObject.Settings.Sections.Select(s => s.Name),
                    RegistrationBuilder.KeyNameBuilder.SectionAndKeyName);
        }
    }
}