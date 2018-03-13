using System.Fabric;
using FG.Diagnostics.AutoLogger.Model;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
    public class ServiceContextTypeTemplateExtension : BaseTemplateExtension<System.Fabric.ServiceContext>
    {
        protected override void BuildArguments(TypeTemplate<System.Fabric.ServiceContext> config)
        {
            config
                .AddArgument(x => x.ServiceName.ToString())
                .AddArgument(x => x.ServiceTypeName)
                .AddArgument("replicaOrInstanceId", x => x.ReplicaOrInstanceId)
                .AddArgument(x => x.PartitionId)
                .AddArgument(x => x.CodePackageActivationContext.ApplicationName)
                .AddArgument(x => x.CodePackageActivationContext.ApplicationTypeName)
                .AddArgument(x => x.NodeContext.NodeName)
                ;
        }

        public override string Module => @"ServiceFabric";
    }    
}