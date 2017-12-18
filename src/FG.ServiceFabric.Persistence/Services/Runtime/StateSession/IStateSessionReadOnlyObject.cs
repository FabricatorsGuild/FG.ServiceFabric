namespace FG.ServiceFabric.Services.Runtime.StateSession
{
    public interface IStateSessionReadOnlyObject
    {
        string Schema { get; }
        bool IsReadOnly { get; }
    }
}