using System.Reflection;
using Microsoft.ServiceFabric.Services.Remoting;

namespace FG.ServiceFabric.Testing.Mocks.Services.Remoting.Client
{
    public interface IMockServiceProxyManager
    {
        void BeforeMethod(IService service, MethodInfo method);
        void AfterMethod(IService service, MethodInfo method);
    }
}