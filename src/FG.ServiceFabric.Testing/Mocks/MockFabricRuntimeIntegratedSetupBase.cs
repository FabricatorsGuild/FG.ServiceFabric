using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FG.Common.Utils;
using FG.ServiceFabric.Services.Runtime.StateSession;
using FG.ServiceFabric.Testing.Mocks.Actors.Client;
using FG.ServiceFabric.Testing.Mocks.Services.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FG.ServiceFabric.Testing.Mocks
{
	public class MockFabricRuntimeIntegratedSetupBase
	{

		public class ServiceManifext
		{
			public string[] ServiceTypes { get; set; }

			public static ServiceManifext Load(string serviceManifestPath)
			{
				var xdoc = XDocument.Load(serviceManifestPath);
				var xns = (XNamespace)"http://schemas.microsoft.com/2011/01/fabric";

				var statelessServiceTypeElements =
					xdoc.Element(xns + "ServiceManifest")?.Element(xns + "ServiceTypes")?.Elements(xns + "StatelessServiceType")
						?.Select(e => e.Attribute("ServiceTypeName")?.Value).Where(e => e != null) ?? new string[0];
				var statefulServiceTypeElements =
					xdoc.Element(xns + "ServiceManifest")?.Element(xns + "ServiceTypes")?.Elements(xns + "StatefulServiceType")
						?.Select(e => e.Attribute("ServiceTypeName")?.Value).Where(e => e != null) ?? new string[0];

				var serviceManifext = new ServiceManifext();
				serviceManifext.ServiceTypes = statefulServiceTypeElements.Union(statelessServiceTypeElements).ToArray();

				return serviceManifext;
			}
		}

		public class ServiceProject
		{
			public MockFabricApplication FabricApplication { get; set; }
			public string ApplicationName { get; set; }
			public string Name { get; set; }
			public string[] ServiceTypes { get; set; }
			public string OutputPath { get; set; }
			public IDictionary<string, MockServiceDefinition> ServiceDefinitions { get; } = new ConcurrentDictionary<string, MockServiceDefinition>();
			public Assembly Assembly { get; set; }


			public static ServiceProject Load(string serviceProjectPath)
			{

				var projectXDoc = XDocument.Load(serviceProjectPath);

				var isCpsProject = projectXDoc.Element("Project")?.Attribute("Sdk")?.Value != null;

				var serviceProject = isCpsProject ? LoadCPSProject(projectXDoc, serviceProjectPath) : LoadClassicProject(projectXDoc, serviceProjectPath);

				var projectBasePath = System.IO.Path.GetDirectoryName(serviceProjectPath);
				var serviceManifestPath = PathExtensions.GetAbsolutePath(projectBasePath, "PackageRoot/ServiceManifest.xml");
				var serivceManifest = ServiceManifext.Load(serviceManifestPath);
				serviceProject.ServiceTypes = serivceManifest.ServiceTypes.ToArray();

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

		public class ApplicationProject
		{
			public ServiceProject[] ServiceProjects { get; set; }

			public static ApplicationProject Load(string applicationProjectPath)
			{
				var xns = (XNamespace)"http://schemas.microsoft.com/developer/msbuild/2003";
				var projectXDoc = XDocument.Load(applicationProjectPath);
				var projectBasePath = System.IO.Path.GetDirectoryName(applicationProjectPath);

				var noneIncludes = projectXDoc.Element(xns + "Project")?.Elements(xns + "ItemGroup").Elements(xns + "None")
					                   .Select(e => e.Attribute("Include")?.Value)
					                   .Where(e => e != null)
					                   .Select((e => PathExtensions.GetAbsolutePath(projectBasePath, e))).ToArray() ?? new string[0];

				var applicationManifestPath = noneIncludes.FirstOrDefault(i => i.EndsWith("ApplicationManifest.xml"));

				var projectReferenceIncludes = projectXDoc.Element(xns + "Project")?.Elements(xns + "ItemGroup").Elements(xns + "ProjectReference")
					                               .Select(e => e.Attribute("Include")?.Value)
					                               .Where(e => e != null)
					                               .Select((e => PathExtensions.GetAbsolutePath(projectBasePath, e))).ToArray() ?? new string[0];


				var applicationProject = new ApplicationProject();

				var serviceProjects = new List<ServiceProject>();
				foreach (var projectReferenceInclude in projectReferenceIncludes)
				{
					var projectReferencePath = PathExtensions.GetAbsolutePath(projectBasePath, projectReferenceInclude);

					var serviceProject = ServiceProject.Load(projectReferencePath);
					serviceProjects.Add(serviceProject);
				}

				applicationProject.ServiceProjects = serviceProjects.ToArray();

				return applicationProject;
			}
		}

		public class ApplicationManifestDefaultService
		{
			public string Name { get; set; }
			public string TypeName { get; set; }
			public bool IsStateless { get; set; }

			public int InstanceCount { get; set; }
			public int TargetReplicaSetSize { get; set; }
			public int MinReplicaSetSize { get; set; }

			public ServicePartitionKind ServicePartitionKind { get; set; }

			public int? PartitionCount { get; set; }
			public long? PartitioningLowLey { get; set; }
			public long? PartitioningHighLey { get; set; }

			public string[] NamedPartitions { get; set; }
		}

		public class ApplicationManifest
		{
			private static string GetStringParameterValue(string unresolvedValue, IDictionary<string, string> defaultValues, IDictionary<string, string> deploymentValues)
			{
				if (unresolvedValue.Matches(@"\[[^\]]*\]", StringComparison.InvariantCulture, useWildcards: false))
				{
					var parameterKey = unresolvedValue.Substring(1, unresolvedValue.Length - 2);
					if (deploymentValues.ContainsKey(parameterKey))
					{
						return deploymentValues[parameterKey];
					}
					if (defaultValues.ContainsKey(parameterKey))
					{
						return defaultValues[parameterKey];
					}
				}
				return unresolvedValue;
			}
			private static int GetIntParameterValue(string unresolvedValue, IDictionary<string, string> defaultValues, IDictionary<string, string> deploymentValues)
			{
				var resolvedValue = GetStringParameterValue(unresolvedValue, defaultValues, deploymentValues);
				if (int.TryParse(resolvedValue, out var value))
				{
					return value;
				}
				throw new ArgumentOutOfRangeException($"Value {unresolvedValue}/{resolvedValue} could not be resolved as an int");
			}
			private static long GetLongParameterValue(string unresolvedValue, IDictionary<string, string> defaultValues, IDictionary<string, string> deploymentValues)
			{
				var resolvedValue = GetStringParameterValue(unresolvedValue, defaultValues, deploymentValues);
				if (long.TryParse(resolvedValue, out var value))
				{
					return value;
				}
				throw new ArgumentOutOfRangeException($"Value {unresolvedValue}/{resolvedValue} could not be resolved as an long");
			}

			private static int? GetIntValue(XElement element, string attributeName, IDictionary<string, string> defaultParameters, IDictionary<string, string> deploymentParameters)
			{
				var attributeValue = element.Attribute(attributeName)?.Value;
				return attributeValue != null
					? (int?)GetIntParameterValue(
						element.Attribute(attributeName)?.Value,
						defaultParameters,
						deploymentParameters)
					: null;
			}

			private static long? GetLongValue(XElement element, string attributeName, IDictionary<string, string> defaultParameters, IDictionary<string, string> deploymentParameters)
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

			public ApplicationManifestDefaultService[] DefaultServices { get; set; }

			public static ApplicationManifest Load(string applicationManifestPath, string applicationParametersPath)
			{
				var applicationManifest = new ApplicationManifest();

				var xns = (XNamespace)"http://schemas.microsoft.com/2011/01/fabric";
				var xdoc = XDocument.Load(applicationManifestPath);

				var applicationTypeName = xdoc.Element(xns + "ApplicationManifest").Attribute("ApplicationTypeName").Value;
				applicationManifest.ApplicationName = applicationTypeName.RemoveFromEnd("Type");


				var xdocParameters = XDocument.Load(applicationParametersPath);
				var deploymentParameters = xdocParameters.Element(xns + "Application").Element(xns + "Parameters").Elements(xns + "Parameter")
					.ToDictionary(e => e.Attribute("Name").Value, e => e.Attribute("Value").Value);

				var manifestElement = xdoc.Element(xns + "ApplicationManifest");
				var defaultParameters = manifestElement.Element(xns + "Parameters").Elements(xns + "Parameter")
					.ToDictionary(e => e.Attribute("Name").Value, e => e.Attribute("DefaultValue").Value);

				var serviceManifests = manifestElement.Elements(xns + "ServiceManifestImport");
				var defaultServiceElementss = manifestElement.Element(xns + "DefaultServices").Elements(xns + "Service");

				var getIntValue = (Func<XElement, string, int?>)((element, attributeName) => GetIntValue(element, attributeName, defaultParameters, deploymentParameters));
				var getLongValue = (Func<XElement, string, long?>)((element, attributeName) => GetLongValue(element, attributeName, defaultParameters, deploymentParameters));

				var defaultServices = new List<ApplicationManifestDefaultService>();
				foreach (var defaultServiceElement in defaultServiceElementss)
				{
					var applicationManifestDefaultService = new ApplicationManifestDefaultService();

					applicationManifestDefaultService.Name = defaultServiceElement.Attribute("Name").Value;
					var statelessService = defaultServiceElement.Element(xns + "StatelessService");
					var statefulService = defaultServiceElement.Element(xns + "StatefulService");
					applicationManifestDefaultService.IsStateless = (statelessService != null);

					var serviceElement = applicationManifestDefaultService.IsStateless ? statelessService : statefulService;
					applicationManifestDefaultService.TypeName = serviceElement.Attribute("ServiceTypeName").Value;

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
						var singletonPartition = serviceElement.Element(xns + "SingletonPartition");
						var uniformInt64Partition = serviceElement.Element(xns + "UniformInt64Partition");
						var namedPartition = serviceElement.Element(xns + "NamedPartition");

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

		private class ServiceProjectAssemblyResolver
		{
			public ServiceProject ServiceProject { get; set; }

			public Assembly Resolve(string reference)
			{
				if (ServiceProject == null) return null;

				var referenceName = reference.Split(',')[0];
				var assemblyFile = Directory
					.GetFiles(ServiceProject.OutputPath)
					.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == referenceName);
				if (assemblyFile != null)
				{
					return Assembly.LoadFrom(assemblyFile);
				}
				return null;
			}
		}

		protected void Setup(MockFabricRuntime mockFabricRuntime, string applicationProjectPath, string applicationManifestPath = null, string applicationParametersPath = null)
		{
			var applicationProjectFolder = System.IO.Path.GetDirectoryName(applicationProjectPath);

			if (applicationManifestPath == null)
			{
				applicationManifestPath = PathExtensions.GetAbsolutePath(applicationProjectFolder, @"ApplicationPackageRoot\ApplicationManifest.xml");
			}

			if (applicationParametersPath == null)
			{
				applicationParametersPath = PathExtensions.GetAbsolutePath(applicationProjectFolder, @"ApplicationParameters\Cloud.xml");
			}


			var applicationManifest = ApplicationManifest.Load(applicationManifestPath, applicationParametersPath);
			var applicationProject = ApplicationProject.Load(applicationProjectPath);

			var mockFabricApplication = mockFabricRuntime.RegisterApplication(applicationManifest.ApplicationName);

			var serviceProjects = new List<ServiceProject>();
			foreach (var defaultService in applicationManifest.DefaultServices)
			{
				MockServiceDefinition serviceDefinition = null;

				if (defaultService.IsStateless)
				{
					serviceDefinition = MockServiceDefinition.CreateStateless(defaultService.InstanceCount);
				}
				else
				{
					if (defaultService.ServicePartitionKind == ServicePartitionKind.Int64Range)
					{
						serviceDefinition = MockServiceDefinition.CreateUniformInt64Partitions(defaultService.PartitionCount ?? 10, defaultService.PartitioningLowLey ?? long.MinValue, defaultService.PartitioningHighLey ?? long.MaxValue);
					}
					else if (defaultService.ServicePartitionKind == ServicePartitionKind.Named)
					{
						serviceDefinition = MockServiceDefinition.CreateNamedPartitions(defaultService.NamedPartitions);
					}
					else
					{
						serviceDefinition = MockServiceDefinition.CreateSingletonPartition();
					}
				}

				var serviceProject = applicationProject.ServiceProjects.FirstOrDefault(s => s.ServiceTypes.Any(t => t == defaultService.TypeName));
				if (serviceProject == null)
				{
					throw new MockFabricSetupException(
						$"Could not find ServiceType {defaultService.TypeName} in referenced services in the Application {applicationManifest.ApplicationName}");
				}
				if (!serviceProjects.Contains(serviceProject))
				{
					serviceProject.ApplicationName = applicationManifest.ApplicationName;
					serviceProject.FabricApplication = mockFabricApplication;
					serviceProjects.Add(serviceProject);
				}
				serviceProject.ServiceDefinitions.Add(defaultService.TypeName, serviceDefinition);
			}

			ServiceProject.LoadAssemblies(this.GetType(), serviceProjects);
			foreach (var serviceProject in serviceProjects)
			{
				var actorTypes = serviceProject.Assembly.GetTypes().Where(type => typeof(IActor).IsAssignableFrom(type)).ToDictionary(t => t.Name, t => t);
				var actorServiceTypes = serviceProject.Assembly.GetTypes().Where(type => typeof(ActorService).IsAssignableFrom(type)).ToDictionary(t => t.Name, t => t);
				var statelessServiceTypes = serviceProject.Assembly.GetTypes().Where(type => typeof(StatelessService).IsAssignableFrom(type)).ToDictionary(t => t.Name, t => t);
				var statefulServiceTypes = serviceProject.Assembly.GetTypes().Where(type => typeof(StatefulService).IsAssignableFrom(type)).ToDictionary(t => t.Name, t => t);

				foreach (var serviceDefinition in serviceProject.ServiceDefinitions)
				{
					var serviceName = serviceDefinition.Key.RemoveFromEnd("Type");
					var actorName = serviceDefinition.Key.RemoveFromEnd("ServiceType");

					// TODO: Make this consider ActorServiceName attribute for actorTypes as well and not just naive name/string matching
					if (actorTypes.ContainsKey(actorName))
					{
						var actorType = actorTypes[actorName];
						var actorServiceType = actorServiceTypes.ContainsKey(serviceName) ? actorServiceTypes[serviceName] : typeof(ActorService);
						Console.WriteLine($"Create ActorService {serviceName} for type {actorName}/{actorServiceType.FullName} - {serviceDefinition}");

						SetupActor(serviceProject.FabricApplication, actorType, actorServiceType, serviceDefinition.Value);
					}
					else if (actorServiceTypes.ContainsKey(serviceName))
					{
						var actorServiceType = actorServiceTypes[serviceName];
						var actorTypeNameBeginning = serviceName.RemoveFromEnd("Service").RemoveFromEnd("Actor");
						var likelyActorType = actorTypes.Values.FirstOrDefault(actorType => actorType.Name.StartsWith(actorTypeNameBeginning));
						Console.WriteLine($"Create ActorService {serviceName} for type {actorName}/{likelyActorType?.FullName} - {serviceDefinition}");

						SetupActor(serviceProject.FabricApplication, likelyActorType, actorServiceType, serviceDefinition.Value);
					}
					else if (statefulServiceTypes.ContainsKey(serviceName))
					{
						var serviceType = statefulServiceTypes[serviceName];
						Console.WriteLine($"Create StatefulService {serviceName} for type {statefulServiceTypes[serviceName].FullName} - {serviceDefinition}");

						SetupStatefulService(serviceProject.FabricApplication, serviceType, serviceDefinition.Value);
					}
					else if (statelessServiceTypes.ContainsKey(serviceName))
					{
						var serviceType = statelessServiceTypes[serviceName];
						Console.WriteLine($"Create Stateless {serviceName} for type {statelessServiceTypes[serviceName].FullName} - {serviceDefinition}");

						SetupStatelessService(serviceProject.FabricApplication, serviceType, serviceDefinition.Value);
					}
				}
			}
		}

		private ActorBase CreateActor(Type actorType, ActorService actorService, ActorId actorId)
		{
			var maxArgsConstructor = actorType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
			if (maxArgsConstructor == null) throw new MockFabricSetupException($"Could not find a .ctor for Actor {actorType.Name}");

			var parameters = new List<object>();
			foreach (var constructorParameter in maxArgsConstructor.GetParameters())
			{
				var parameterType = constructorParameter.ParameterType;

				if (typeof(ActorService).IsAssignableFrom(parameterType))
				{
					parameters.Add(actorService);
				}
				else if (typeof(ActorId).IsAssignableFrom(parameterType))
				{
					parameters.Add(actorId);
				}
				else
				{
					var parameter = CreateActorParameter(actorType, actorService, actorId, parameterType);
					if (parameter == null) throw new MockServiceRuntimeException($"Trying to setup Actor {actorType.Name} but test class does not override CreateActorParameter or the override returns null");
					parameters.Add(parameter);
				}
			}

			var actor = maxArgsConstructor.Invoke(parameters.ToArray()) as ActorBase;
			return actor;
		}

		protected virtual object CreateActorParameter(Type actorType, ActorService actorService, ActorId actorId, Type parameterType) { return null; }

		private ActorService CreateActorService(Type actorServiceType, ActorTypeInformation actorTypeInformation, StatefulServiceContext context, IActorStateProvider stateProvider, Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory)
		{
			var maxArgsConstructor = actorServiceType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
			if (maxArgsConstructor == null) throw new MockFabricSetupException($"Could not find a .ctor for ServiceType {actorServiceType.Name}");

			var parameters = new List<object>();
			foreach (var constructorParameter in maxArgsConstructor.GetParameters())
			{
				var parameterType = constructorParameter.ParameterType;

				if (typeof(IActorStateProvider).IsAssignableFrom(parameterType))
				{
					parameters.Add(stateProvider);
				}
				else if (typeof(StatefulServiceContext).IsAssignableFrom(parameterType))
				{
					parameters.Add(context);
				}
				else if (typeof(ActorTypeInformation).IsAssignableFrom(parameterType))
				{
					parameters.Add(actorTypeInformation);
				}
				else if (typeof(Func<ActorBase, IActorStateProvider, IActorStateManager>).IsAssignableFrom(parameterType))
				{
					parameters.Add(stateManagerFactory);
				}
				else if (typeof(IStateSessionManager).IsAssignableFrom(parameterType))
				{
					parameters.Add(CreateStateSessionManager(context));
				}
				else
				{
					parameters.Add(CreateActorServiceParameter(actorServiceType, parameterType));
				}
			}

			var actorService = maxArgsConstructor.Invoke(parameters.ToArray()) as ActorService;
			return actorService;
		}

		protected virtual Func<ActorBase, IActorStateProvider, IActorStateManager> CreateActorStateManagerFactory() { return null; }

		protected virtual IActorStateProvider CreateActorStateProvider(StatefulServiceContext context, ActorTypeInformation actorTypeInformation) { return null; }

		protected virtual object CreateActorServiceParameter(Type actorServiceType, Type parameterType) { return null; }

		protected virtual IStateSessionManager CreateStateSessionManager(ServiceContext context) { return null; }

		private void SetupActor(MockFabricApplication fabricApplication, Type actorType, Type actorServiceType, MockServiceDefinition serviceDefinition)
		{
			var actorStateManagerFactory = CreateActorStateManagerFactory();

			fabricApplication.SetupActor(
				actorImplementationType: actorType,
				actorServiceImplementationType: actorServiceType,
				activator: (actorService, actorId) => CreateActor(actorType, actorService, actorId),
				createActorService: (context, information, stateProvider, stateManagerFactory) => CreateActorService(actorServiceType, information, context, stateProvider, stateManagerFactory),
				createActorStateManager: actorStateManagerFactory != null ? (CreateActorStateManager)((actor, provider) => actorStateManagerFactory(actor, provider)) : null,
				createActorStateProvider: CreateActorStateProvider,
				serviceDefinition: serviceDefinition);
		}

		private void SetupStatelessService(MockFabricApplication fabricApplication, Type serviceType, MockServiceDefinition serviceDefinition)
		{
			fabricApplication.SetupService(
				serviceType,
				createService: context => CreateStatelessService(serviceType, context),
				serviceDefinition: serviceDefinition);
		}

		private void SetupStatefulService(MockFabricApplication fabricApplication, Type serviceType, MockServiceDefinition serviceDefinition)
		{
			fabricApplication.SetupService(
				serviceType,
				createService: (context, stateManagerReplica) => CreateStatefulService(serviceType, context, stateManagerReplica),
				createStateManager: () => CreateStateManager(serviceType, serviceDefinition),
				serviceDefinition: serviceDefinition);
		}

		private StatefulService CreateStatefulService(Type serviceType, StatefulServiceContext context, IReliableStateManagerReplica2 stateManagerReplica)
		{
			var maxArgsConstructor = serviceType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
			if (maxArgsConstructor == null) throw new MockFabricSetupException($"Could not find a .ctor for ServiceType {serviceType.Name}");

			var parameters = new List<object>();
			foreach (var constructorParameter in maxArgsConstructor.GetParameters())
			{
				var parameterType = constructorParameter.ParameterType;
				if (typeof(StatefulServiceContext).IsAssignableFrom(parameterType))
				{
					parameters.Add(context);
				}
				else if (typeof(StatefulServiceContext).IsAssignableFrom(parameterType))
				{
					parameters.Add(context);
				}
				else if (typeof(IReliableStateManagerReplica2).IsAssignableFrom(parameterType))
				{
					parameters.Add(stateManagerReplica);
				}
				else if (typeof(IStateSessionManager).IsAssignableFrom(parameterType))
				{
					parameters.Add(CreateStateSessionManager(context));
				}
				else
				{
					var parameter = CreateServiceParameter(context, serviceType, parameterType);
					if (parameter == null) throw new MockServiceRuntimeException($"Trying to setup StatefulService {serviceType.Name} but test class does not override CreateServiceParameter or the override returns null");
					parameters.Add(parameter);
				}
			}

			var service = maxArgsConstructor.Invoke(parameters.ToArray()) as StatefulService;
			return service;
		}

		private StatelessService CreateStatelessService(Type serviceType, StatelessServiceContext context)
		{
			var maxArgsConstructor = serviceType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
			if (maxArgsConstructor == null) throw new MockFabricSetupException($"Could not find a .ctor for ServiceType {serviceType.Name}");

			var parameters = new List<object>();
			foreach (var constructorParameter in maxArgsConstructor.GetParameters())
			{
				var parameterType = constructorParameter.ParameterType;
				if (typeof(StatelessServiceContext).IsAssignableFrom(parameterType))
				{
					parameters.Add(context);
				}
				else if (typeof(IStateSessionManager).IsAssignableFrom(parameterType))
				{
					parameters.Add(CreateStateSessionManager(context));
				}
				else
				{
					var parameter = CreateServiceParameter(context, serviceType, parameterType);
					if (parameter == null) throw new MockServiceRuntimeException($"Trying to setup StatelessService {serviceType.Name} but test class does not override CreateServiceParameter or the override returns null");
					parameters.Add(parameter);
				}
			}

			var service = maxArgsConstructor.Invoke(parameters.ToArray()) as StatelessService;
			return service;
		}

		protected virtual IReliableStateManagerReplica2 CreateStateManager(Type serviceType, MockServiceDefinition serviceDefinition) { return null; }

		protected virtual object CreateServiceParameter(StatefulServiceContext context, Type serviceType, Type parameterType) { return null; }

		protected virtual object CreateServiceParameter(StatelessServiceContext context, Type serviceType, Type parameterType) { return null; }
	}
}