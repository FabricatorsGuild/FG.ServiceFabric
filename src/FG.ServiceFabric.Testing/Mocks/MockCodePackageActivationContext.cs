using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Health;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Setup;

namespace FG.ServiceFabric.Testing.Mocks
{
    public class MockCodePackageActivationContext : ICodePackageActivationContext
    {
        private readonly object _lock = new object();
        private readonly IServiceConfig _serviceConfig;
        private readonly IServiceManifest _serviceManifest;

        private ConfigurationPackage _configurationPackage;

        public MockCodePackageActivationContext(
            string applicationName,
            string applicationTypeName,
            string codePackageName,
            string codePackageVersion,
            string context,
            string logDirectory,
            string tempDirectory,
            string workDirectory,
            IServiceManifest serviceManifest,
            IServiceConfig serviceConfig)
        {
            _serviceManifest = serviceManifest;
            _serviceConfig = serviceConfig;
            ApplicationName = applicationName;
            ApplicationTypeName = applicationTypeName;
            CodePackageName = codePackageName;
            CodePackageVersion = codePackageVersion;
            ContextId = context;
            LogDirectory = logDirectory;
            TempDirectory = tempDirectory;
            WorkDirectory = workDirectory;
            ServiceManifestName = _serviceManifest.Name;
            ServiceManifestVersion = _serviceManifest.Version;
        }

        private string ServiceManifetName { get; set; }

        private string ServiceManifestVersion { get; }
        public string ServiceManifestName { get; }
        public string ApplicationName { get; }

        public string ApplicationTypeName { get; }

        public string CodePackageName { get; }

        public string CodePackageVersion { get; }

        public string ContextId { get; }

        public string LogDirectory { get; }

        public string TempDirectory { get; }

        public string WorkDirectory { get; }

        public ApplicationPrincipalsDescription GetApplicationPrincipals()
        {
            throw new NotImplementedException();
        }

        public IList<string> GetCodePackageNames()
        {
            return new List<string> {CodePackageName};
        }

        public CodePackage GetCodePackageObject(string packageName)
        {
            throw new NotImplementedException();
        }

        public IList<string> GetConfigurationPackageNames()
        {
            return new List<string> {"Config"};
        }

        public ConfigurationPackage GetConfigurationPackageObject(string packageName)
        {
            if (packageName.Equals("Config", StringComparison.InvariantCultureIgnoreCase))
                lock (_lock)
                {
                    if (_configurationPackage != null)
                        return _configurationPackage;

                    var configurationPackage = ReflectionUtils.ActivateInternalCtor<ConfigurationPackage>();
                    var configurationSettings = ReflectionUtils.ActivateInternalCtor<ConfigurationSettings>();
                    configurationPackage.SetPrivateProperty(() => configurationPackage.Settings, configurationSettings);
                    foreach (var serviceConfigSection in _serviceConfig.Sections)
                    {
                        var configurationSection = ReflectionUtils.ActivateInternalCtor<ConfigurationSection>();
                        configurationSection.SetPrivateProperty(() => configurationSection.Name,
                            serviceConfigSection.Name);

                        var parameters = new MockConfigurationSectionParametersCollection();
                        foreach (var parameter in serviceConfigSection.Parameters)
                        {
                            var configParameter = ReflectionUtils.ActivateInternalCtor<ConfigurationProperty>();
                            configParameter.SetPrivateProperty(() => configParameter.Name, parameter.Value);

                            parameters.Add(configParameter);
                        }
                        configurationPackage.Settings.Sections.Add(configurationSection);
                    }
                    _configurationPackage = configurationPackage;

                    return _configurationPackage;
                }
            throw new NotImplementedException();
        }

        public IList<string> GetDataPackageNames()
        {
            return new List<string> {""};
        }

        public DataPackage GetDataPackageObject(string packageName)
        {
            throw new NotImplementedException();
        }

        public EndpointResourceDescription GetEndpoint(string endpointName)
        {
            throw new NotImplementedException();
        }

        public KeyedCollection<string, EndpointResourceDescription> GetEndpoints()
        {
            throw new NotImplementedException();
        }

        public KeyedCollection<string, ServiceGroupTypeDescription> GetServiceGroupTypes()
        {
            throw new NotImplementedException();
        }

        public string GetServiceManifestName()
        {
            return ServiceManifetName;
        }

        public string GetServiceManifestVersion()
        {
            return ServiceManifestVersion;
        }

        public KeyedCollection<string, ServiceTypeDescription> GetServiceTypes()
        {
            throw new NotImplementedException();
        }

        public void ReportApplicationHealth(HealthInformation healthInformation)
        {
            throw new NotImplementedException();
        }

        public void ReportDeployedServicePackageHealth(HealthInformation healthInformation)
        {
            throw new NotImplementedException();
        }

        public void ReportDeployedApplicationHealth(HealthInformation healthInformation)
        {
            throw new NotImplementedException();
        }

#pragma warning disable 0067
        public event EventHandler<PackageAddedEventArgs<CodePackage>> CodePackageAddedEvent;
        public event EventHandler<PackageModifiedEventArgs<CodePackage>> CodePackageModifiedEvent;
        public event EventHandler<PackageRemovedEventArgs<CodePackage>> CodePackageRemovedEvent;
        public event EventHandler<PackageAddedEventArgs<ConfigurationPackage>> ConfigurationPackageAddedEvent;
        public event EventHandler<PackageModifiedEventArgs<ConfigurationPackage>> ConfigurationPackageModifiedEvent;
        public event EventHandler<PackageRemovedEventArgs<ConfigurationPackage>> ConfigurationPackageRemovedEvent;
        public event EventHandler<PackageAddedEventArgs<DataPackage>> DataPackageAddedEvent;
        public event EventHandler<PackageModifiedEventArgs<DataPackage>> DataPackageModifiedEvent;
        public event EventHandler<PackageRemovedEventArgs<DataPackage>> DataPackageRemovedEvent;
#pragma warning restore 0067


        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        public void ReportApplicationHealth(HealthInformation healthInfo, HealthReportSendOptions sendOptions)
        {
            throw new NotImplementedException();
        }

        public void ReportDeployedApplicationHealth(HealthInformation healthInfo, HealthReportSendOptions sendOptions)
        {
            throw new NotImplementedException();
        }

        public void ReportDeployedServicePackageHealth(HealthInformation healthInfo,
            HealthReportSendOptions sendOptions)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}