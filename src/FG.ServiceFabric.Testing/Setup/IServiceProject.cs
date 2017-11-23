using FG.ServiceFabric.Testing.Mocks;

namespace FG.ServiceFabric.Testing.Setup
{
	internal interface IServiceProject
	{
		string ApplicationName { get; set; }
		string Name { get; set; }
		IServiceManifest Manifest { get; set; }
		IServiceConfig Config { get; set; }
	}
}