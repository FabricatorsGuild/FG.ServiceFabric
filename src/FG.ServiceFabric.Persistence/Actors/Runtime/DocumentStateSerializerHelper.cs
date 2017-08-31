using System;

namespace FG.ServiceFabric.Actors.Runtime
{
	public static class DocumentStateSerializerHelper
	{
		public static string ToJson(this object value)
		{
			if (value == null) return null;
			return Newtonsoft.Json.JsonConvert.SerializeObject(value);
		}

		public static T FromJson<T>(this string json)
		{
			if (json == null) return default(T);
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
		}

		public static object FromJson(this string json, Type type)
		{
			if (json == null) return null;
			return Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
		}
	}
}