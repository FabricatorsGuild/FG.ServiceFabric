using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{
	public class ExternalState
	{
		public string Key { get; set; }

		// TODO: remove this attribute and add as a json.net converter or contract in the actual persister
		[JsonProperty(TypeNameHandling = TypeNameHandling.Objects)]
		public object Value { get; set; }
	}
}