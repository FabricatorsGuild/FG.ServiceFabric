using FG.Diagnostics.AutoLogger.Model;
using Microsoft.ServiceFabric.Services.Remoting.V1;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
    public class ServiceRemotingMessageHeadersTypeTemplateExtension : BaseTemplateExtension<Microsoft.ServiceFabric.Services.Remoting.V1.ServiceRemotingMessageHeaders>
    {
        protected override void BuildArguments(TypeTemplate<ServiceRemotingMessageHeaders> config)
        {
            config                    
                .AddArgument("interfaceId", x => x.InterfaceId)
                .AddArgument("methodId", x => x.MethodId)
                ;
        }

        public override string Module => @"ServiceFabric";
        
    }
}