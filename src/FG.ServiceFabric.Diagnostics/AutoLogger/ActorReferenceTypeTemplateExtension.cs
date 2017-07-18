//using FG.Diagnostics.AutoLogger.Model;

//namespace FG.ServiceFabric.Diagnostics.AutoLogger
//{
//    public class ActorReferenceTypeTemplateExtension : BaseTemplateExtension
//    {        
//        private string Definition = @"{
//              ""Name"": ""ActorReference"",
//              ""CLRType"": ""Microsoft.ServiceFabric.Actors"",
//              ""Arguments"": [
//                {
//                  ""Assignment"": ""$this.Uri.ToString()"",
//                  ""Name"": ""serviceUri"",
//                  ""Type"": ""string"",
//                  ""CLRType"": ""string""
//                },
//                {
//                  ""Assignment"": ""$this.ActorId.ToString()"",
//                  ""Name"": ""actorId"",
//                  ""Type"": ""string"",
//                  ""CLRType"": ""string""
//                }
//              ]
//            }";

//        protected override string GetDefinition()
//        {
//            return Definition;
//		}
//		public override string Module => @"ServiceFabric";
//	}
//}