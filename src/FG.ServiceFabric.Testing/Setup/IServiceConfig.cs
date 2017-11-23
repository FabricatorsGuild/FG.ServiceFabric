namespace FG.ServiceFabric.Testing.Setup
{
	public interface IServiceConfig
	{
		IServiceConfigSection[] Sections { get; set; }
	}
}