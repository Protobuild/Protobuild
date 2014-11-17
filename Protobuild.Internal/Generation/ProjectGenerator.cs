using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Protobuild
{
    using System.Runtime.InteropServices.ComTypes;
    using Protobuild.Services;

    public class ProjectGenerator : IProjectGenerator
    {
        private readonly IResourceProvider m_ResourceProvider;

        private readonly INuGetConfigMover m_NuGetConfigMover;

        private readonly IProjectInputGenerator m_ProjectInputGenerator;

        private readonly IExcludedServiceAwareProjectDetector m_ExcludedServiceAwareProjectDetector;

        private readonly IExternalProjectReferenceResolver m_ExternalProjectReferenceResolver;

        public ProjectGenerator(
            IResourceProvider resourceProvider,
            INuGetConfigMover nuGetConfigMover,
            IProjectInputGenerator projectInputGenerator,
            IExcludedServiceAwareProjectDetector excludedServiceAwareProjectDetector,
            IExternalProjectReferenceResolver externalProjectReferenceResolver)
        {
            this.m_ResourceProvider = resourceProvider;
            this.m_NuGetConfigMover = nuGetConfigMover;
            this.m_ProjectInputGenerator = projectInputGenerator;
            this.m_ExcludedServiceAwareProjectDetector = excludedServiceAwareProjectDetector;
            this.m_ExternalProjectReferenceResolver = externalProjectReferenceResolver;
        }

        /// <summary>
        /// Generates a project at the target path.
        /// </summary>
        /// <param name="project">The name of the project to generate.</param>
        /// <param name="services"></param>
        /// <param name="packagesFilePath">
        /// Either the full path to the packages.config for the
        /// generated project if it exists, or an empty string.
        /// </param>
        /// <param name="onActualGeneration"></param>
        public void Generate(
            List<XmlDocument> documents,
            string rootPath,
            string projectName,
            string platformName,
            List<Service> services,
            out string packagesFilePath,
            Action onActualGeneration)
        {
            packagesFilePath = string.Empty;

            var projectTransform = this.m_ResourceProvider.LoadXSLT(ResourceType.GenerateProject, Language.CSharp);

            // Work out what document this is.
            var projectDoc = documents.First(
                x => x.DocumentElement.Attributes["Name"].Value == projectName);

            // Check to see if we have a Project node; if not
            // then this is an external or other type of project
            // that we don't process.
            if (projectDoc == null ||
                projectDoc.DocumentElement.Name != "Project")
                return;

            // Work out what platforms this project should be generated for.
            var platformAttribute = projectDoc.DocumentElement.Attributes["Platforms"];
            string[] allowedPlatforms = null;
            if (platformAttribute != null)
            {
                allowedPlatforms = platformAttribute.Value
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();
            }

            // Filter on allowed platforms.
            if (allowedPlatforms != null) 
            {
                var allowed = false;
                foreach (var platform in allowedPlatforms)
                {
                    if (string.Compare(platformName, platform, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        allowed = true;
                        break;
                    }
                }
                if (!allowed)
                {
                    return;
                }
            }

            // If the project has a <Services> node, but there are no entries in /Input/Services,
            // then nothing depends on this service (if there is a <Reference> to this project, it will
            // use the default service).  So for service-aware projects without any services being
            // referenced, we exclude them from the generation.
            if (this.m_ExcludedServiceAwareProjectDetector.IsExcludedServiceAwareProject(
                projectDoc.DocumentElement.Attributes["Name"].Value,
                projectDoc,
                services))
            {
                return;
            }

            // Inform the user we're generating this project.
            onActualGeneration();

            // Imply external project references from other external projects.  We do
            // this so that external projects can reference other external projects (which
            // we can't reasonably handle at the XSLT level since it's recursive).
            this.m_ExternalProjectReferenceResolver.ResolveExternalProjectReferences(documents, projectDoc);

            // Work out what path to save at.
            var path = Path.Combine(
                rootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar),
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                platformName + ".csproj");

            // Make sure that the directory exists where the file will be stored.
            var targetFile = new FileInfo(path);
            if (!targetFile.Directory.Exists)
                targetFile.Directory.Create();

            path = targetFile.FullName;

            // Handle NuGet packages.config early so that it'll be in place
            // when the generator automatically determined dependencies.
            this.m_NuGetConfigMover.Move(rootPath, platformName, projectDoc);

            // Work out what path the NuGet packages.config might be at.
            var packagesFile = new FileInfo(
                Path.Combine(
                    rootPath,
                    projectDoc.DocumentElement.Attributes["Path"].Value
                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar),
                    "packages.config"));

            // Generate the input document.
            var input = this.m_ProjectInputGenerator.Generate(
                documents,
                rootPath,
                projectName,
                platformName,
                packagesFile.FullName,
                projectDoc.DocumentElement.ChildNodes
                    .OfType<XmlElement>()
                    .Where(x => x.Name.ToLower() == "properties")
                    .SelectMany(x => x.ChildNodes
                        .OfType<XmlElement>()),
                services);

            // Transform the input document using the XSLT transform.
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var writer = XmlWriter.Create(path, settings))
            {
                projectTransform.Transform(input, writer);
            }

            // Also remove any left over .sln or .userprefs files.
            var slnPath = Path.Combine(
                rootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value,
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                platformName + ".sln");
            var userprefsPath = Path.Combine(
                rootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value,
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                platformName + ".userprefs");
            if (File.Exists(slnPath))
                File.Delete(slnPath);
            if (File.Exists(userprefsPath))
                File.Delete(userprefsPath);

            // Only return the package file path if it exists.
            if (packagesFile.Exists)
                packagesFilePath = packagesFile.FullName;
        }
    }
}

