using System.Collections.Generic;

namespace FG.ServiceFabric.Services.Remoting.FabricTransport
{
    public interface IServiceRequestContextWrapperHandler
    {
        IEnumerable<string> GetKeys();
    }
}