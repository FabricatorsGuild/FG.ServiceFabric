using FG.Diagnostics.AutoLogger.Model;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
	public class CustomServiceRequestHeaderTypeTemplateExtension : BaseTemplateExtension
	{
		private string Definition = @"{
                  ""Name"": ""CustomServiceRequestHeader"",
                  ""CLRType"": ""FG.ServiceFabric.Services.Remoting.FabricTransport.CustomServiceRequestHeader"",
                  ""Arguments"": [
                    {
                      ""Name"": ""userId"",
                      ""Type"": ""string"",
                      ""Assignment"": ""$this?.GetHeader(\""userId\"")""
                    },
                    {
                      ""Name"": ""correlationId"",
                      ""Type"": ""string"",
                      ""Assignment"": ""$this?.GetHeader(\""correlationId\"")""
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