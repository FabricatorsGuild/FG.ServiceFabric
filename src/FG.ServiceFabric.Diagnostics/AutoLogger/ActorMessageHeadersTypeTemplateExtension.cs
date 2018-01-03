using FG.Diagnostics.AutoLogger.Model;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
    public class ActorMessageHeadersTypeTemplateExtension : BaseTemplateExtension
    {
        private readonly string Definition = @"{
                  ""Name"": ""ActorMessageHeaders"",
                  ""CLRType"": ""FG.ServiceFabric.Actors.Remoting.Runtime.ActorMessageHeaders"",
                  ""Arguments"": [
                    {
                      ""Name"": ""interfaceId"",
                      ""Type"": ""int"",
                      ""Assignment"": ""($this?.InterfaceId ?? 0)""
                    },
                    {
                      ""Name"": ""methodId"",
                      ""Type"": ""int"",
                      ""Assignment"": ""($this?.MethodId ?? 0)""
                    },
                    {
                      ""Name"": ""actorId"",
                      ""Type"": ""string"",
                      ""Assignment"": ""$this?.ActorId.ToString()""
                    }
                  ]
                }";

        public override string Module => @"ServiceFabric";

        protected override string GetDefinition()
        {
            return Definition;
        }
    }
}