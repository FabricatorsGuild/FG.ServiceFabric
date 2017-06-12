using CodeEffect.Diagnostics.EventSourceGenerator.Model;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Diagnostics.LoggerTypeTemplates
{
    public class ActorServiceTypeTemplateExtension : BaseTemplateExtension<ActorService>
    {
        private string Definition = @"{
                  ""Name"": ""ActorService"",
                  ""CLRType"": ""Microsoft.ServiceFabric.Actors.Runtime.ActorService"",
                  ""Arguments"": [
                    {
                      ""Assignment"": ""$this.ActorTypeInformation.ImplementationType.ToString()"",
                      ""Name"": ""actorType"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.Context.CodePackageActivationContext.ApplicationTypeName"",
                      ""Name"": ""applicationTypeName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.Context.CodePackageActivationContext.ApplicationName"",
                      ""Name"": ""applicationName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.Context.ServiceTypeName"",
                      ""Name"": ""serviceTypeName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.Context.ServiceName.ToString()"",
                      ""Name"": ""serviceName"",
                      ""Type"": ""string"",
                      ""CLRType"": ""string""
                    },
                    {
                      ""Assignment"": ""$this.Context.PartitionId"",
                      ""Name"": ""partitionId"",
                      ""Type"": ""Guid"",
                      ""CLRType"": ""Guid""
                    },
                    {
                      ""Assignment"": ""$this.Context.ReplicaId"",
                      ""Name"": ""replicaOrInstanceId"",
                      ""Type"": ""long"",
                      ""CLRType"": ""long""
                    },
                    {
                      ""Assignment"": ""$this.Context.NodeContext.NodeName"",
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
    }
}