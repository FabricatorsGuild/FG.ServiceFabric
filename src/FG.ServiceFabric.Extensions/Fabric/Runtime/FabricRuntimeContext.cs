using FG.Common.CallContext;

namespace FG.ServiceFabric.Fabric.Runtime
{
    // ReSharper disable once ClassNeverInstantiated.Global - Created in using classes
    public sealed class FabricRuntimeContext : BaseCallContext<FabricRuntimeContext, object>
    {
        private FabricRuntimeContext()
        {
        }

        public static FabricRuntimeContext Current { get; } = new FabricRuntimeContext();
    }
}