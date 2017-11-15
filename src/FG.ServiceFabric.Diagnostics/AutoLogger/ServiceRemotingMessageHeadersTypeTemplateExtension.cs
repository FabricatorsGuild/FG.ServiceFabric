using FG.Diagnostics.AutoLogger.Model;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
	public class ServiceRemotingMessageHeadersTypeTemplateExtension : BaseTemplateExtension
	{
		private string Definition = @"{
                  ""Name"": ""ServiceRemotingMessageHeaders"",
                  ""CLRType"": ""Microsoft.ServiceFabric.Services.Remoting.V1.ServiceRemotingMessageHeaders"",
                  ""Arguments"": [
                    {
                      ""Name"": ""InterfaceId"",
                      ""Type"": ""int"",
                      ""Assignment"": ""($this?.InterfaceId ?? 0)""
                    },
                    {
                      ""Name"": ""MethodId"",
                      ""Type"": ""int"",
                      ""Assignment"": ""($this?.MethodId ?? 0)""
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