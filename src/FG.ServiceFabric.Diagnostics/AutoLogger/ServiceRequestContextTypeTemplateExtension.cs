using FG.Diagnostics.AutoLogger.Model;

namespace FG.ServiceFabric.Diagnostics.AutoLogger
{
    public class ServiceRequestContextTypeTemplateExtension : BaseTemplateExtension
    {
        private string Definition = @"{
              ""Name"": ""ServiceRequestContext"",
              ""CLRType"": ""FG.ServiceFabric.Services.Remoting.FabricTransport.ServiceRequestContext"",
              ""Arguments"": [
                {
                  ""Assignment"": ""FG.ServiceFabric.Services.Remoting.FabricTransport.ServiceRequestContext.Current?[\""correlationId\""]"",
                  ""Name"": ""correlationId"",
                  ""Type"": ""string""
                },
                {
                  ""Assignment"": ""FG.ServiceFabric.Services.Remoting.FabricTransport.ServiceRequestContext.Current?[\""userId\""]"",
                  ""Name"": ""userId"",
                  ""Type"": ""string""
                },
                {
                  ""Assignment"": ""FG.ServiceFabric.Services.Remoting.FabricTransport.ServiceRequestContext.Current?[\""requestUri\""]"",
                  ""Name"": ""requestUri"",
                  ""Type"": ""string""
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