namespace FG.ServiceFabric.Fabric.Runtime
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.Remoting.Messaging;

    using FG.Common.CallContext;

    // ReSharper disable once ClassNeverInstantiated.Global - Created in using classes
    public sealed class FabricRuntimeContext : BaseCallContext<FabricRuntimeContext, object>
    {
        private FabricRuntimeContext()
        {
        }

        public static FabricRuntimeContext Current { get; } = new FabricRuntimeContext();
    }
}