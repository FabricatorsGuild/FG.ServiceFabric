namespace FG.ServiceFabric.Fabric.Runtime
{
    using FG.Common.CallContext;

    public class FabricRuntimeContextWrapper : BaseCallContextWrapper<FabricRuntimeContext, object>
    {
        public static FabricRuntimeContextWrapper Current => new FabricRuntimeContextWrapper();

        public IServiceRuntimeRegistration ServiceRuntimeRegistration
        {
            get => (IServiceRuntimeRegistration)FabricRuntimeContext.Current?[FabricRuntimeContextKeys.ServiceRuntimeRegistration];
            set => this.Context?.SetItem(FabricRuntimeContextKeys.ServiceRuntimeRegistration, value);
        }

        public object this[string key]
        {
            get => FabricRuntimeContext.Current?[key];
            set => FabricRuntimeContext.Current[key] = value;
        }

        private static class FabricRuntimeContextKeys
        {
            public const string ServiceRuntimeRegistration = "ServiceRuntimeRegistration";
        }
    }
}