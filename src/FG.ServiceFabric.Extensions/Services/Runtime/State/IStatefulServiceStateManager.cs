namespace FG.ServiceFabric.Services.Runtime.State
{
    public interface IStatefulServiceStateManager
    {
        IStatefulServiceStateManagerSession CreateSession();
    }
}