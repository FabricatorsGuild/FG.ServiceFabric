namespace FG.ServiceFabric.Testing.Assertion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class ObjectPropertyAssertions
    {
        public static void CheckAllMatchingProperties(
            this object baseObject,
            object compareTo,
            Action<string, object, object> actionToPerformForEachPropertyChecked,
            IDictionary<string, string> propertyNameTransforms,
            string[] ignoredProperties = null)
        {
            var firstSetOfProperties = baseObject.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(pi => (ignoredProperties ?? Array.Empty<string>()).All(p => !string.Equals(p, pi.Name, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(pi => pi.Name, pi => pi, StringComparer.OrdinalIgnoreCase);

            var secondSetOfProperties = compareTo.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(pi => pi.Name, pi => pi, StringComparer.OrdinalIgnoreCase);

            var caseInsensitivePropertyNameTransforms = propertyNameTransforms.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

            foreach (var firstSetPropertyKey in firstSetOfProperties.Keys)
            {
                if (caseInsensitivePropertyNameTransforms.TryGetValue(firstSetPropertyKey, out var secondSetPropertyKey) == false)
                {
                    secondSetPropertyKey = firstSetPropertyKey;
                }

                var firstSetProperty = firstSetOfProperties[firstSetPropertyKey];

                if (!secondSetOfProperties.TryGetValue(secondSetPropertyKey, out var secondSetProperty))
                {
                    throw new ArgumentException($"Missing property {secondSetPropertyKey} on B object");
                }

                var firstValue = firstSetProperty.GetValue(baseObject);
                var secondValue = secondSetProperty.GetValue(compareTo);

                actionToPerformForEachPropertyChecked(firstSetPropertyKey, firstValue, secondValue);
            }
        }

        public static void CheckMatchingProperties(this object a, object b, Action<string, object, object> checkProperty, params string[] propertyNames)
        {
            var aProperties = a.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).ToDictionary(pi => pi.Name, pi => pi);
            var bProperties = b.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).ToDictionary(pi => pi.Name, pi => pi);

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