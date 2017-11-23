namespace FG.ServiceFabric.Testing.Setup
{
	public interface IServiceManifest
	{
		string Name { get; set; }
		string Version { get; set; }
		string[] ServiceTypes { get; set; }
	}
}