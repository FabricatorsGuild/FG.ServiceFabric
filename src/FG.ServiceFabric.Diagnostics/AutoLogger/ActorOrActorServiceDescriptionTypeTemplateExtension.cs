using FG.Diagnostics.AutoLogger.Model;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
    public class ActorOrActorServiceDescriptionTypeTemplateExtension : BaseTemplateExtension
    {        
        private string Definition = @"{
              ""Name"": ""ActorOrActorServiceDescription"",
              ""CLRType"": ""FG.ServiceFabric.Diagnostics.ActorOrActorServiceDescription"",
              ""Arguments"": [
                {
                  ""Assignment"": ""$this.ActorType.ToString()"",
                  ""Name"": ""actorType"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.ActorId.ToString()"",
                  ""Name"": ""actorId"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.ApplicationTypeName"",
                  ""Name"": ""applicationTypeName"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.ApplicationName"",
                  ""Name"": ""applicationName"",
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
                  ""Assignment"": ""$this.ServiceName"",
                  ""Name"": ""serviceName"",
                  ""Type"": ""string"",
                  ""CLRType"": ""string""
                },
                {
                  ""Assignment"": ""$this.PartitionId"",
                  ""Name"": ""partitionId"",
                  ""Type"": ""Guid"",
                  ""CLRType"": ""Guid""
                },
                {
                  ""Assignment"": ""$this.ReplicaOrInstanceId"",
                  ""Name"": ""replicaOrInstanceId"",
                  ""Type"": ""long"",
                  ""CLRType"": ""long""
                },
                {
                  ""Assignment"": ""$this.NodeName"",
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