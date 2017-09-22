namespace FG.ServiceFabric.Utils
{
    public interface ISettingsProvider
    {
	    bool Contains(string key);
        string this[string key] { get; }
		string[] Keys { get; }
    }
}