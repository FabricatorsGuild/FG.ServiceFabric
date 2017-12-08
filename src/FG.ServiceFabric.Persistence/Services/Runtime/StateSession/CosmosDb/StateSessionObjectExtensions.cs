using System.Text;
using FG.Common.Utils;

namespace FG.ServiceFabric.Services.Runtime.StateSession.CosmosDb
{
	public static class StateSessionObjectExtensions
	{
		public static string AsList(this IStateSessionReadOnlyObject[] stateSessionObjects)
		{
			var stringBuilder = new StringBuilder();
			var delimiter = "";
			foreach (var stateSessionObject in stateSessionObjects)
			{
				stringBuilder.Append($"{stateSessionObject.Schema}:{stateSessionObject.GetType().GetFriendlyName()}{delimiter}");
				delimiter = ",";
			}
			return stringBuilder.ToString();
		}
	}
}