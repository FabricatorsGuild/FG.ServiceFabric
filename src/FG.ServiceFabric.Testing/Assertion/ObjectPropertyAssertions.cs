using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FG.ServiceFabric.Testing.Assertion
{
	public static class ObjectPropertyAssertions
	{
		public static void CheckAllMatchingProperties(this object baseObject, object compareTo,
			Action<string, object, object> actionToPerformForEachPropertyChecked,
			IDictionary<string, string> propertyNameTransforms, string[] ignoredProperties = null)
		{
			var aProperties = baseObject.GetType()
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.Where(pi =>
					(ignoredProperties ?? new string[] { }).All(p =>
						!string.Equals(p, pi.Name, StringComparison.InvariantCultureIgnoreCase)))
				.ToDictionary((pi) => pi.Name.ToLower(), pi => pi);
			var bProperties = compareTo.GetType()
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.ToDictionary((pi) => pi.Name.ToLower(), pi => pi);

			var caseInsensitivePropertyNameTransforms =
				propertyNameTransforms.ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value.ToLower());

			foreach (var aPropertyName in aProperties.Keys)
			{
				var bPropertyName = caseInsensitivePropertyNameTransforms.ContainsKey(aPropertyName.ToLower())
					? caseInsensitivePropertyNameTransforms[aPropertyName]
					: aPropertyName;

				var aProperty = aProperties[aPropertyName];
				if (!bProperties.ContainsKey(bPropertyName))
					throw new ArgumentException($"Missing property {bPropertyName} on B object");
				var bProperty = bProperties[bPropertyName];

				var aValue = aProperty.GetValue(baseObject);
				var bValue = bProperty.GetValue(compareTo);

				actionToPerformForEachPropertyChecked(aPropertyName, aValue, bValue);
			}
		}

		public static void CheckMatchingProperties(this object a, object b, Action<string, object, object> checkProperty,
			params string[] propertyNames)
		{
			var aProperties = a.GetType()
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.ToDictionary((pi) => pi.Name, pi => pi);
			var bProperties = b.GetType()
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.ToDictionary((pi) => pi.Name, pi => pi);

			foreach (var propertyName in propertyNames)
			{
				var aProperty = aProperties[propertyName];
				var bProperty = bProperties[propertyName];

				var aValue = aProperty.GetValue(a);
				var bValue = bProperty.GetValue(b);

				checkProperty(propertyName, aValue, bValue);
			}
		}
	}
}