namespace FG.Common.Settings
{
    public interface ISettingsProvider
    {
        string this[string key] { get; }
        string[] Keys { get; }
        bool Contains(string key);
    }
}