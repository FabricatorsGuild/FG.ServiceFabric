using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Xml.Linq;
using FG.Common.Utils;

namespace FG.ServiceFabric.Testing.Setup
{
    public class ApplicationParameters
    {
        private IDictionary<string, string> _values = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, string> Values => (IReadOnlyDictionary<string, string>)_values;

        public string this[string name]
        {
            get => _values[name];
            set => _values[name] = value;
        }

        public static ApplicationParameters Load(string applicationParametersPath)
        {
            var xns = (XNamespace)"http://schemas.microsoft.com/2011/01/fabric";

            var xdocParameters = XDocument.Load(applicationParametersPath);
            var deploymentParameters = xdocParameters.Element(xns + "Application").Element(xns + "Parameters").Elements(xns + "Parameter")
                .ToDictionary(e => e.Attribute("Name").Value, e => e.Attribute("Value").Value);

            return new ApplicationParameters() { _values = deploymentParameters };

        }
    }

    internal class ApplicationManifest
    {

        private static string GetStringParameterValue(string unresolvedValue, IReadOnlyDictionary<string, string> defaultValues, IReadOnlyDictionary<string, string> deploymentValues)
        {
            if (unresolvedValue.Matches(@"\[[^\]]*\]", StringComparison.InvariantCulture, useWildcards: false))
            {
                var parameterKey = unresolvedValue.Substring(1, unresolvedValue.Length - 2);
                if (deploymentValues.TryGetValue(parameterKey, out var deploymentValue))
                {
                    return deploymentValue;
                }

                if (defaultValues.TryGetValue(parameterKey, out var defaultValue))
                {
                    return defaultValue;
                }
            }

            return unresolvedValue;
        }
        private static int GetIntParameterValue(string unresolvedValue, IReadOnlyDictionary<string, string> defaultValues, IReadOnlyDictionary<string, string> deploymentValues)
        {
            var resolvedValue = GetStringParameterValue(unresolvedValue, defaultValues, deploymentValues);
            if (int.TryParse(resolvedValue, out var value))
            {
                return value;
            }
            throw new ArgumentOutOfRangeException($"Value {unresolvedValue}/{resolvedValue} could not be resolved as an int");
        }
        private static long GetLongParameterValue(string unresolvedValue, IReadOnlyDictionary<string, string> defaultValues, IReadOnlyDictionary<string, string> deploymentValues)
        {
            var resolvedValue = GetStringParameterValue(unresolvedValue, defaultValues, deploymentValues);
            if (long.TryParse(resolvedValue, out var value))
            {
                return value;
            }
            throw new ArgumentOutOfRangeException($"Value {unresolvedValue}/{resolvedValue} could not be resolved as an long");
        }

        private static int? GetIntValue(XElement element, string attributeName, IReadOnlyDictionary<string, string> defaultParameters, IReadOnlyDictionary<string, string> deploymentParameters)
        {
            var attributeValue = element.Attribute(attributeName)?.Value;
            return attributeValue != null
                ? (int?)GetIntParameterValue(
                    element.Attribute(attributeName)?.Value,
                    defaultParameters,
                    deploymentParameters)
                : null;
        }

        private static long? GetLongValue(XElement element, string attributeName, IReadOnlyDictionary<string, string> defaultParameters, IReadOnlyDictionary<string, string> deploymentParameters)
        {
            var attributeValue = element.Attribute(attributeName)?.Value;
            return attributeValue != null
                ? (long?)GetLongParameterValue(
                    element.Attribute(attributeName)?.Value,
                    defaultParameters,
                    deploymentParameters)
                : null;
        }

        public string ApplicationName { get; set; }

        public ApplicationManifestServiceManifestImport[] ServiceManifestImports { get; set; }

        public ApplicationManifestDefaultService[] DefaultServices { get; set; }

        public static ApplicationManifest Load(string applicationManifestPath, string applicationParametersPath)
        {
            var applicationParameters = ApplicationParameters.Load(applicationParametersPath);
            return Load(applicationManifestPath, applicationParameters);
        }

        public static ApplicationManifest Load(string applicationManifestPath, ApplicationParameters applicationParameters)
        {
            var applicationManifest = new ApplicationManifest();

            var xns = (XNamespace)"http://schemas.microsoft.com/2011/01/fabric";
            var xdoc = XDocument.Load(applicationManifestPath);

            var applicationTypeName = xdoc.Element(xns + "ApplicationManifest").Attribute("ApplicationTypeName").Value;
            applicationManifest.ApplicationName = applicationTypeName.RemoveFromEnd("Type");

            var manifestElement = xdoc.Element(xns + "ApplicationManifest");
            var defaultParameters = manifestElement.Element(xns + "Parameters").Elements(xns + "Parameter")
                .ToDictionary(e => e.Attribute("Name").Value, e => e.Attribute("DefaultValue").Value);

            var getIntValue = (Func<XElement, string, int?>)((element, attributeName) => GetIntValue(element, attributeName, defaultParameters, applicationParameters.Values));
            var getLongValue = (Func<XElement, string, long?>)((element, attributeName) => GetLongValue(element, attributeName, defaultParameters, applicationParameters.Values));

            var serviceManifestImportElements = manifestElement.Elements(xns + "ServiceManifestImport");
            var serviceManifestImports = new List<ApplicationManifestServiceManifestImport>();
            foreach (var serviceManifestImportElement in serviceManifestImportElements)
            {
                var serviceManifestRefElement = serviceManifestImportElement.Element(xns + "ServiceManifestRef");
                var name = serviceManifestRefElement?.Attribute("ServiceManifestName")?.Value;
                var version = serviceManifestRefElement?.Attribute("ServiceManifestVersion")?.Value;

                var configOverrideSettingsSectionElements = serviceManifestImportElement.Elements(xns + "ConfigOverrides")
                    ?.Elements(xns + "ConfigOverride").Where(element => element.Attribute("Name")?.Value == "Config")
                    ?.Elements(xns + "Settings")?.Elements(xns + "Section");

                var serviceManifestImportConfigSections = new List<ApplicationManifestServiceManifestImport.ConfigSection>();
                foreach (var sectionElement in configOverrideSettingsSectionElements)
                {
                    var sectionName = sectionElement.Attribute("Name")?.Value;
                    var parameters = sectionElement.Elements(xns + "Parameter").Select((parameterElement, i) =>
                        new KeyValuePair<string, string>(
                            parameterElement.Attribute("Name")?.Value ?? $"parameter{i}",
                            GetStringParameterValue(parameterElement.Attribute("Value")?.Value ?? "", defaultParameters, applicationParameters.Values)));

                    var configSection =
                        new ApplicationManifestServiceManifestImport.ConfigSection()
                        {
                            Name = sectionName,
                            Parameters = parameters.ToArray()
                        };
                    serviceManifestImportConfigSections.Add(configSection);
                }

                var serviceManifestImport = new ApplicationManifestServiceManifestImport() { Name = name, Version = version, Sections = serviceManifestImportConfigSections.ToArray() };
                serviceManifestImports.Add(serviceManifestImport);
            }
            applicationManifest.ServiceManifestImports = serviceManifestImports.ToArray();

            var defaultServiceElements = manifestElement.Element(xns + "DefaultServices")?.Elements(xns + "Service") ?? new XElement[0];
            var defaultServices = new List<ApplicationManifestDefaultService>();
            foreach (var defaultServiceElement in defaultServiceElements)
            {
                var applicationManifestDefaultService = new ApplicationManifestDefaultService();

                applicationManifestDefaultService.Name = defaultServiceElement.Attribute("Name")?.Value;
                var statelessService = defaultServiceElement.Element(xns + "StatelessService");
                var statefulService = defaultServiceElement.Element(xns + "StatefulService");
                applicationManifestDefaultService.IsStateless = (statelessService != null);

                var serviceElement = applicationManifestDefaultService.IsStateless ? statelessService : statefulService;
                applicationManifestDefaultService.TypeName = serviceElement?.Attribute("ServiceTypeName")?.Value;

                applicationManifestDefaultService.InstanceCount = getIntValue(serviceElement, "InstanceCount") ?? 0;
                applicationManifestDefaultService.InstanceCount = applicationManifestDefaultService.InstanceCount == -1 ? 10 : applicationManifestDefaultService.InstanceCount;
                applicationManifestDefaultService.TargetReplicaSetSize = getIntValue(serviceElement, "TargetReplicaSetSize") ?? 3;
                applicationManifestDefaultService.MinReplicaSetSize = getIntValue(serviceElement, "MinReplicaSetSize") ?? 1;

                if (applicationManifestDefaultService.IsStateless)
                {
                    applicationManifestDefaultService.ServicePartitionKind = ServicePartitionKind.Singleton;
                }
                else
                {
                    var singletonPartition = serviceElement?.Element(xns + "SingletonPartition");
                    var uniformInt64Partition = serviceElement?.Element(xns + "UniformInt64Partition");
                    var namedPartition = serviceElement?.Element(xns + "NamedPartition");

                    if (uniformInt64Partition != null)
                    {
                        applicationManifestDefaultService.ServicePartitionKind = ServicePartitionKind.Int64Range;
                        applicationManifestDefaultService.PartitioningLowLey = getLongValue(uniformInt64Partition, "LowKey") ?? long.MinValue;
                        applicationManifestDefaultService.PartitioningHighLey = getLongValue(uniformInt64Partition, "HighKey") ?? long.MaxValue;
                        applicationManifestDefaultService.PartitionCount = getIntValue(uniformInt64Partition, "PartitionCount") ?? 10;
                    }

                    if (namedPartition != null)
                    {
                        applicationManifestDefaultService.ServicePartitionKind = ServicePartitionKind.Named;
                        applicationManifestDefaultService.NamedPartitions = namedPartition.Elements(xns + "Partition").Select(partition => partition.Attribute("Name").Value).ToArray();
                    }

                    if (singletonPartition != null)
                    {
                        applicationManifestDefaultService.ServicePartitionKind = ServicePartitionKind.Singleton;
                    }
                }
                defaultServices.Add(applicationManifestDefaultService);
            }
            applicationManifest.DefaultServices = defaultServices.ToArray();


            return applicationManifest;
        }
    }
}