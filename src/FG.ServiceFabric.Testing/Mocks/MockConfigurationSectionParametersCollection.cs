using System.Collections.ObjectModel;
using System.Fabric.Description;

namespace FG.ServiceFabric.Testing.Mocks
{
	public class MockConfigurationSectionParametersCollection : KeyedCollection<string, ConfigurationProperty>
	{
		protected override string GetKeyForItem(ConfigurationProperty item)
		{
			return item.Name;
		}
	}
}