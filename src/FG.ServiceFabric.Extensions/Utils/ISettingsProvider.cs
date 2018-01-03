namespace FG.ServiceFabric.Utils
{
    public interface ISettingsProvider
    {
        string this[string key] { get; }
        string[] Keys { get; }
        bool Contains(string key);
    }
}