namespace FG.ServiceFabric.Utils
{
    public interface ISettingsProvider
    {
        string this[string key] { get; }
    }
}