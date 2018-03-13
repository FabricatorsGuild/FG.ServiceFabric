using FG.Diagnostics.AutoLogger.Model;
using FG.ServiceFabric.Services.Remoting.FabricTransport;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
    public class CustomServiceRequestHeaderTypeTemplateExtension : BaseTemplateExtension<FG.ServiceFabric.Services.Remoting.FabricTransport.CustomServiceRequestHeader>
    {
        protected override void BuildArguments(TypeTemplate<CustomServiceRequestHeader> config)
        {
            config
                .AddArgument("userId", x => x != null ? x.GetHeader("userId") : "")
                .AddArgument("correlationId", x => x != null ? x.GetHeader("correlationId") : "")
                ;
        }

        public override string Module => @"ServiceFabric";
    }
}