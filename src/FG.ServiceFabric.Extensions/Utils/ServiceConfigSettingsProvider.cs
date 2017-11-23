using System.Fabric;
using System.Linq;

namespace FG.ServiceFabric.Utils
{
	public class ServiceConfigSettingsProvider : SettingsProviderBase
	{
		public ServiceConfigSettingsProvider(ServiceContext context) : base(context)
		{
			var configurationPackageNames = context.CodePackageActivationContext.GetConfigurationPackageNames();
			if (!configurationPackageNames.Contains("config"))
			{
				return;
			}

			var configurationPackageObject = context.CodePackageActivationContext.GetConfigurationPackageObject("config");

			Configure()
				.FromSettings(configurationPackageObject.Settings.Sections.Select(s => s.Name),
					RegistrationBuilder.KeyNameBuilder.SectionAndKeyName);
		}
	}
}