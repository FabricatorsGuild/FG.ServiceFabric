using CodeEffect.Diagnostics.EventSourceGenerator.Model;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Diagnostics.LoggerTypeTemplates
{
    public class ActorTypeTemplateExtension : BaseTemplateExtension<Actor>
    {        
        private string Definition = @"{
              ""Name"": ""Actor"",
              ""CLRType"": ""Microsoft.ServiceFabric.Actors.Runtime.Actor"",
              ""Arguments"": [
                {
                  ""Assignment"": ""$this.GetType().ToString()"",
                  ""Name"": ""actorType"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.Id.ToString()"",
                  ""Name"": ""actorId"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.ActorService.Context.CodePackageActivationContext.ApplicationTypeName"",
                  ""Name"": ""applicationTypeName"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.ActorService.Context.CodePackageActivationContext.ApplicationName"",
                  ""Name"": ""applicationName"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.ActorService.Context.ServiceTypeName"",
                  ""Name"": ""serviceTypeName"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.ActorService.Context.ServiceName.ToString()"",
                  ""Name"": ""serviceName"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.ActorService.Context.PartitionId"",
                  ""Name"": ""partitionId"",
                  ""Type"": ""Guid"",
                  ""CLRType"": ""Guid""
                },
                {
                  ""Assignment"": ""$this.ActorService.Context.ReplicaId"",
                  ""Name"": ""replicaOrInstanceId"",
                  ""Type"": ""long"",
                  ""CLRType"": ""long""
                },
                {
                  ""Assignment"": ""$this.ActorService.Context.NodeContext.NodeName"",
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