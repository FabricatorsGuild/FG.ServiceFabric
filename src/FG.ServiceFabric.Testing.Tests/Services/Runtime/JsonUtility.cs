using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FG.ServiceFabric.Testing.Tests.Services.Runtime.With_StateSessionManager
{
    public class JsonUtility
    {
        public static string GetNormalizedJson(object value)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            return NormalizeJsonString(json);
        }

        public static string CullProperties(string json, Func<string, bool> shouldBeInclued)
        {
            // Parse json string into JObject.
            var parsedObject = JObject.Parse(json);

            // Sort properties of JObject.
            var normalizedObject = CullProperties(parsedObject, shouldBeInclued);

            // Serialize JObject .
            return JsonConvert.SerializeObject(normalizedObject);
        }

        public static string NormalizeJsonString(string json)
        {
            // Parse json string into JObject.
            var parsedObject = JObject.Parse(json);

            // Sort properties of JObject.
            var normalizedObject = SortPropertiesAlphabetically(parsedObject);

            // Serialize JObject .
            return JsonConvert.SerializeObject(normalizedObject);
        }

        private static JObject CullProperties(JObject original, Func<string, bool> shouldBeInclued)
        {
            var result = new JObject();

            foreach (var property in original.Properties().Where(p => shouldBeInclued(p.Name)).ToList().OrderBy(p => p.Name))
            {
                if (property.Value is JObject value)
                {
                    value = SortPropertiesAlphabetically(value);
                    result.Add(property.Name, value);
                }
                else
                {
                    result.Add(property.Name, property.Value);
                }
            }

            return result;
        }

        private static JObject SortPropertiesAlphabetically(JObject original)
        {
            var result = new JObject();

            foreach (var property in original.Properties().ToList().OrderBy(p => p.Name))
            {
                if (property.Value is JObject value)
                {
                    value = SortPropertiesAlphabetically(value);
                    result.Add(property.Name, value);
                }
                else
                {
                    result.Add(property.Name, property.Value);
                }
            }

            return result;
        }
    }
}