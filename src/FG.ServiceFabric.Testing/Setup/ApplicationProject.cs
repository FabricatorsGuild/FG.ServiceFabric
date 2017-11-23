using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FG.Common.Utils;

namespace FG.ServiceFabric.Testing.Setup
{
	internal class ApplicationProject
	{
		public string ApplicationManifestPath { get; set; }

		public IServiceProject[] ServiceProjects { get; set; }

		public static ApplicationProject Load(string applicationProjectPath)
		{
			var xns = (XNamespace)"http://schemas.microsoft.com/developer/msbuild/2003";
			var projectXDoc = XDocument.Load(applicationProjectPath);
			var projectBasePath = System.IO.Path.GetDirectoryName(applicationProjectPath);

			var noneIncludes = projectXDoc.Element(xns + "Project")?.Elements(xns + "ItemGroup").Elements(xns + "None")
				                   .Select(e => e.Attribute("Include")?.Value)
				                   .Where(e => e != null)
				                   .Select((e => PathExtensions.GetAbsolutePath(projectBasePath, e))).ToArray() ?? new string[0];

			var projectReferenceIncludes = projectXDoc.Element(xns + "Project")?.Elements(xns + "ItemGroup").Elements(xns + "ProjectReference")
				                               .Select(e => e.Attribute("Include")?.Value)
				                               .Where(e => e != null)
				                               .Select((e => PathExtensions.GetAbsolutePath(projectBasePath, e))).ToArray() ?? new string[0];

			var applicationProject = new ApplicationProject();

			var serviceProjects = new List<IServiceProject>();
			foreach (var projectReferenceInclude in projectReferenceIncludes)
			{
				var projectReferencePath = PathExtensions.GetAbsolutePath(projectBasePath, projectReferenceInclude);

				var serviceProject = ServiceProject.Load(projectReferencePath);
				serviceProjects.Add(serviceProject);
			}

			applicationProject.ServiceProjects = serviceProjects.ToArray();
			applicationProject.ApplicationManifestPath = noneIncludes.FirstOrDefault(i => i.EndsWith("ApplicationManifest.xml"));

			applicationProject.ApplicationManifestPath  = applicationProject.ApplicationManifestPath ?? PathExtensions.GetAbsolutePath(projectBasePath, @"ApplicationPackageRoot\ApplicationManifest.xml");

			return applicationProject;
		}
	}
}