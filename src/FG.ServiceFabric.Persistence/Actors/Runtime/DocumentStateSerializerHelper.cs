using System;
using Newtonsoft.Json;

namespace FG.ServiceFabric.Actors.Runtime
{
    public static class DocumentStateSerializerHelper
    {
        public static string ToJson(this object value)
        {
            if (value == null) return null;
            return JsonConvert.SerializeObject(value);
        }

        public static T FromJson<T>(this string json)
        {
            if (json == null) return default(T);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static object FromJson(this string json, Type type)
        {
            if (json == null) return null;
            return JsonConvert.DeserializeObject(json, type);
        }
    }
}