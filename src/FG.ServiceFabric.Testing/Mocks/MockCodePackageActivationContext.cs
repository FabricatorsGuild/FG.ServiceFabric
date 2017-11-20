using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Health;
using FG.Common.Utils;

namespace FG.ServiceFabric.Testing.Mocks
{
	public class MockCodePackageActivationContext : ICodePackageActivationContext
	{
		public MockCodePackageActivationContext(
			string applicationName,
			string applicationTypeName,
			string codePackageName,
			string codePackageVersion,
			string context,
			string logDirectory,
			string tempDirectory,
			string workDirectory,
			string serviceManifestName,
			string serviceManifestVersion)
		{
			this.ApplicationName = applicationName;
			this.ApplicationTypeName = applicationTypeName;
			this.CodePackageName = codePackageName;
			this.CodePackageVersion = codePackageVersion;
			this.ContextId = context;
			this.LogDirectory = logDirectory;
			this.TempDirectory = tempDirectory;
			this.WorkDirectory = workDirectory;
			this.ServiceManifestName = serviceManifestName;
			this.ServiceManifestVersion = serviceManifestVersion;
		}

		private string ServiceManifetName { get; set; }

		private string ServiceManifestVersion { get; set; }
		public string ApplicationName { get; private set; }

		public string ApplicationTypeName { get; private set; }

		public string CodePackageName { get; private set; }

		public string CodePackageVersion { get; private set; }

		public string ContextId { get; private set; }

		public string LogDirectory { get; private set; }

		public string TempDirectory { get; private set; }

		public string WorkDirectory { get; private set; }
		public string ServiceManifestName { get; }

		public ApplicationPrincipalsDescription GetApplicationPrincipals()
		{
			throw new NotImplementedException();
		}

		public IList<string> GetCodePackageNames()
		{
			return new List<string>() {this.CodePackageName};
		}

		public CodePackage GetCodePackageObject(string packageName)
		{
			throw new NotImplementedException();
		}

		public IList<string> GetConfigurationPackageNames()
		{
			return new List<string>() {"config"};
		}

		public ConfigurationPackage GetConfigurationPackageObject(string packageName)
		{
			if (packageName == "config")
			{
				var configurationPackage = ReflectionUtils.ActivateInternalCtor<ConfigurationPackage>();

			}

			throw new NotImplementedException();
		}

		public IList<string> GetDataPackageNames()
		{
			return new List<string>() {""};
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
			return this.ServiceManifetName;
		}

		public string GetServiceManifestVersion()
		{
			return this.ServiceManifestVersion;
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

		private bool disposedValue = false; // To detect redundant calls

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

		public void ReportDeployedServicePackageHealth(HealthInformation healthInfo, HealthReportSendOptions sendOptions)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}