using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FG.Common.Utils;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;

namespace FG.ServiceFabric.Testing.Setup
{
	internal class ServiceProject : IServiceProject
	{
		public MockFabricApplication FabricApplication { get; set; }
		public string ApplicationName { get; set; }
		public string Name { get; set; }
		public IServiceManifest Manifest { get; set; }
		public string OutputPath { get; set; }
		public IDictionary<string, MockServiceDefinition> ServiceDefinitions { get; } = new ConcurrentDictionary<string, MockServiceDefinition>();

		public IServiceConfig Config { get; set; }

		public Assembly Assembly { get; set; }

		public void LoadConfigOverrides(ApplicationManifestServiceManifestImport serviceManifestImport)
		{

			foreach (var serviceConfigSection in Config.Sections)
			{
				var sectionOverride = serviceManifestImport.Sections.FirstOrDefault(section => section.Name == serviceConfigSection.Name);
				if( sectionOverride == null) continue;

				foreach (var parameter in serviceConfigSection.Parameters.ToArray())
				{
					var parameterOverride = sectionOverride.Parameters.FirstOrDefault(p => p.Key == parameter.Key);
					serviceConfigSection.Parameters[parameter.Key] = parameterOverride.Value;
				}
			}

		}

		public static IServiceProject Load(string serviceProjectPath)
		{

			var projectXDoc = XDocument.Load(serviceProjectPath);

			var isCpsProject = projectXDoc.Element("Project")?.Attribute("Sdk")?.Value != null;

			var serviceProject = isCpsProject ? LoadCPSProject(projectXDoc, serviceProjectPath) : LoadClassicProject(projectXDoc, serviceProjectPath);

			var projectBasePath = System.IO.Path.GetDirectoryName(serviceProjectPath);
			var serviceManifestPath = PathExtensions.GetAbsolutePath(projectBasePath, "PackageRoot/ServiceManifest.xml");
			serviceProject.Manifest = ServiceManifest.Load(serviceManifestPath);

			var serviceConfigPath = PathExtensions.GetAbsolutePath(projectBasePath, "PackageRoot/Config/Settings.xml");
			var serviceConfig = ServiceConfig.Load(serviceConfigPath);
			serviceProject.Config = serviceConfig;

			return serviceProject;
		}

		private static ServiceProject LoadClassicProject(XDocument projectXDoc, string serviceProjectPath)
		{
			var serviceProject = new ServiceProject();
			var projectBasePath = System.IO.Path.GetDirectoryName(serviceProjectPath);

			var xns = (XNamespace)"http://schemas.microsoft.com/developer/msbuild/2003";

			serviceProject.Name = projectXDoc.Element(xns + "Project")?.Element(xns + "PropertyGroup")?.Elements(xns + "AssemblyName").FirstOrDefault()?.Value ??
			                      System.IO.Path.GetFileNameWithoutExtension(serviceProjectPath);

			var noneIncludes = projectXDoc.Element(xns + "Project")?.Elements(xns + "ItemGroup").Elements(xns + "None")
				                   .Select(e => e.Attribute("Include")?.Value)
				                   .Where(e => e != null)
				                   .Select((e => PathExtensions.GetAbsolutePath(projectBasePath, e))).ToArray() ?? new string[0];

			var projectReferenceIncludes = projectXDoc.Element(xns + "Project")?.Elements(xns + "ItemGroup").Elements(xns + "ProjectReference")
				                               .Select(e => e.Attribute("Include")?.Value)
				                               .Where(e => e != null)
				                               .Select((e => PathExtensions.GetAbsolutePath(projectBasePath, e))).ToArray() ?? new string[0];

			var outputPath = projectXDoc.Element(xns + "Project")?.Elements(xns + "PropertyGroup").Elements(xns + "OutputPath").FirstOrDefault()?.Value
			                 ?? @"bin\debug";
			serviceProject.OutputPath = PathExtensions.GetAbsolutePath(projectBasePath, outputPath);

			return serviceProject;
		}

		private static ServiceProject LoadCPSProject(XDocument projectXDoc, string serviceProjectPath)
		{
			var serviceProject = new ServiceProject();

			var projectBasePath = System.IO.Path.GetDirectoryName(serviceProjectPath);

			serviceProject.Name = projectXDoc.Element("Project")?.Element("PropertyGroup")?.Elements("AssemblyName").FirstOrDefault()?.Value ??
			                      System.IO.Path.GetFileNameWithoutExtension(serviceProjectPath);

			var targetFramework = projectXDoc.Element("Project")?.Elements("PropertyGroup").Elements("TargetFramework").FirstOrDefault()?.Value;
			var runtimeIdentifier = projectXDoc.Element("Project")?.Elements("PropertyGroup").Elements("RuntimeIdentifier").FirstOrDefault()?.Value;
			var outputPath = projectXDoc.Element("Project")?.Elements("PropertyGroup").Elements("OutputPath").FirstOrDefault()?.Value ?? @"bin\debug\";
			if (targetFramework != null)
			{
				outputPath += $"\\{targetFramework}";
				if (runtimeIdentifier != null)
				{
					outputPath += $"\\{runtimeIdentifier}";
				}
			}
			serviceProject.OutputPath = PathExtensions.GetAbsolutePath(projectBasePath, outputPath);

			return serviceProject;
		}

		public static void LoadAssemblies(Type loaderType, IEnumerable<ServiceProject> serviceProjects)
		{
			var loaderAssembly = loaderType.Assembly;
			var referencedAssemblies = loaderAssembly.GetReferencedAssemblies();

			var projectCodebasePath = new Uri(System.IO.Path.GetDirectoryName(loaderAssembly.CodeBase)).AbsolutePath;
			var assemblyFilesToLoad = System.IO.Directory.GetFiles(projectCodebasePath, "*.dll")
				.Union(System.IO.Directory.GetFiles(projectCodebasePath, "*.exe"))
				.Select(a => new { Name = System.IO.Path.GetFileNameWithoutExtension(a), Assemblyfile = a })
				.Where(a => serviceProjects.Any(p => p.Name.Equals(a.Name)))
				.Where(a => !referencedAssemblies.Any(p => p.Name.Equals(a.Name)))
				.ToArray();

			var additionallyLoadedAssemblies = new List<Assembly>();
			foreach (var assemblyFileToLoad in assemblyFilesToLoad)
			{
				additionallyLoadedAssemblies.Add(Assembly.LoadFrom(assemblyFileToLoad.Assemblyfile));
			}

			var serviceProjectAssemblyResolver = new ServiceProjectAssemblyResolver();
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => serviceProjectAssemblyResolver.Resolve(args.Name);

			foreach (var serviceProject in serviceProjects)
			{

				serviceProjectAssemblyResolver.ServiceProject = serviceProject;

				var assemblyReference = referencedAssemblies.FirstOrDefault(a => a.Name == serviceProject.Name);
				Assembly assembly;
				if (assemblyReference == null)
				{
					assembly = additionallyLoadedAssemblies.FirstOrDefault(a => a.GetName().Name == serviceProject.Name);

					if (assembly == null)
					{
						throw new MockFabricSetupException($"Test project is missing a reference to project {serviceProject.Name} and cannot load the service types in it");
					}
				}
				else
				{
					assembly = Assembly.Load(assemblyReference);
				}
				serviceProject.Assembly = assembly;
			}
		}
	}
}