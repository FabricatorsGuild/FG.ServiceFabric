using FG.Diagnostics.AutoLogger.Model;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
    public class StatefulServiceContextTypeTemplateExtension : BaseTemplateExtension
    {
        private string Definition = @"{
                  ""Name"": ""StatefulServiceContext"",
                  ""CLRType"": ""System.Fabric.StatefulServiceContext"",
                  ""Arguments"": [
                    {
                      ""Assignment"": ""$this.ServiceName.ToString()"",
                      ""Name"": ""serviceName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.ServiceTypeName"",
                      ""Name"": ""serviceTypeName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.ReplicaOrInstanceId"",
                      ""Name"": ""replicaOrInstanceId"",
                      ""Type"": ""long"",
                      ""CLRType"": ""long""
                    },
                    {
                      ""Assignment"": ""$this.PartitionId"",
                      ""Name"": ""partitionId"",
                      ""Type"": ""Guid"",
                      ""CLRType"": ""System.Guid""
                    },
                    {
                      ""Assignment"": ""$this.CodePackageActivationContext.ApplicationName"",
                      ""Name"": ""applicationName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.CodePackageActivationContext.ApplicationTypeName"",
                      ""Name"": ""applicationTypeName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.NodeContext.NodeName"",
                      ""Name"": ""nodeName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    }
                  ]
                }";

        protected override string GetDefinition()
        {
            return Definition;
        }
		public override string Module => @"ServiceFabric";

	}
}