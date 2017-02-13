using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    internal class ProjectGenerator : IProjectGenerator
    {
        private readonly IResourceProvider m_ResourceProvider;

        private readonly INuGetConfigMover m_NuGetConfigMover;

        private readonly IProjectInputGenerator m_ProjectInputGenerator;

        private readonly IExcludedServiceAwareProjectDetector m_ExcludedServiceAwareProjectDetector;

        private readonly IExternalProjectReferenceResolver m_ExternalProjectReferenceResolver;

        private readonly ILanguageStringProvider m_LanguageStringProvider;

        private readonly IPlatformResourcesGenerator _mPlatformResourcesGenerator;

        private readonly IIncludeProjectAppliesToUpdater _includeProjectAppliesToUpdater;

        private readonly IIncludeProjectMerger _includeProjectMerger;

        private readonly IHostPlatformDetector _hostPlatformDetector;

        public ProjectGenerator(
            IResourceProvider resourceProvider,
            INuGetConfigMover nuGetConfigMover,
            IProjectInputGenerator projectInputGenerator,
            IExcludedServiceAwareProjectDetector excludedServiceAwareProjectDetector,
            IExternalProjectReferenceResolver externalProjectReferenceResolver,
            ILanguageStringProvider mLanguageStringProvider,
            IPlatformResourcesGenerator platformResourcesGenerator,
            IIncludeProjectAppliesToUpdater includeProjectAppliesToUpdater,
            IIncludeProjectMerger includeProjectMerger,
            IHostPlatformDetector hostPlatformDetector)
        {
            this.m_ResourceProvider = resourceProvider;
            this.m_NuGetConfigMover = nuGetConfigMover;
            this.m_ProjectInputGenerator = projectInputGenerator;
            this.m_ExcludedServiceAwareProjectDetector = excludedServiceAwareProjectDetector;
            this.m_ExternalProjectReferenceResolver = externalProjectReferenceResolver;
            this.m_LanguageStringProvider = mLanguageStringProvider;
            this._mPlatformResourcesGenerator = platformResourcesGenerator;
            this._includeProjectAppliesToUpdater = includeProjectAppliesToUpdater;
            _includeProjectMerger = includeProjectMerger;
            _hostPlatformDetector = hostPlatformDetector;
        }

        /// <summary>
        /// Generates a project at the target path.
        /// </summary>
        /// <param name="current">The current project to generate.</param>
        /// <param name="definitions">A list of all loaded project definitions.</param>
        /// <param name="workingDirectory"></param>
        /// <param name="rootPath">The module root path, with directory seperator appended.</param>
        /// <param name="projectName">The project name.</param>
        /// <param name="platformName">The platform name.</param>
        /// <param name="services">A list of services.</param>
        /// <param name="packagesFilePath">
        ///     Either the full path to the packages.config for the
        ///     generated project if it exists, or an empty string.
        /// </param>
        /// <param name="onActualGeneration"></param>
        /// <param name="debugProjectGeneration">Whether to emit a .input file for XSLT debugging.</param>
        public void Generate(
            DefinitionInfo current,
            List<LoadedDefinitionInfo> definitions,
            string workingDirectory,
            string rootPath,
            string projectName,
            string platformName,
            List<Service> services,
            out string packagesFilePath,
            Action onActualGeneration,
            bool debugProjectGeneration)
        {
            packagesFilePath = string.Empty;

            // Work out what document this is.
            var projectDoc = definitions.First(
                x => x.Project.DocumentElement.Attributes["Name"].Value == projectName)?.Project;

            // Check to see if we have a Project node; don't process it.
            if (projectDoc?.DocumentElement == null)
                return;

            // If this is a custom project, run any custom generation commands for
            // the current host and target platform.
            if (projectDoc.DocumentElement.Name == "CustomProject")
            {
                onActualGeneration();

                var onGenerateElements = projectDoc.DocumentElement.SelectNodes("OnGenerate");
                if (onGenerateElements != null)
                {
                    foreach (var onGenerateElement in onGenerateElements.OfType<XmlElement>())
                    {
                        var matches = true;
                        var hostName = onGenerateElement.GetAttribute("HostName");
                        var targetName = onGenerateElement.GetAttribute("TargetName");

                        if (!string.IsNullOrWhiteSpace(hostName))
                        {
                            if (hostName != _hostPlatformDetector.DetectPlatform())
                            {
                                matches = false;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(targetName))
                        {
                            if (targetName != platformName)
                            {
                                matches = false;
                            }
                        }

                        if (matches)
                        {
                            var command = onGenerateElement.SelectSingleNode("Command")?.InnerText.Trim();
                            var arguments = onGenerateElement.SelectSingleNode("Arguments")?.InnerText.Trim() ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(command))
                            {
                                var workingPath = Path.Combine(
                                    rootPath,
                                    projectDoc.DocumentElement.Attributes["Path"].Value);
                                var resolvedCommandPath = Path.Combine(
                                    workingPath,
                                    command);

                                if (File.Exists(resolvedCommandPath))
                                {
                                    var process =
                                        Process.Start(new ProcessStartInfo(resolvedCommandPath, arguments)
                                        {
                                            WorkingDirectory = workingPath,
                                            UseShellExecute = false
                                        });
                                    if (process == null)
                                    {
                                        RedirectableConsole.ErrorWriteLine("ERROR: Process did not start when running " + resolvedCommandPath + " " +
                                            arguments);
                                        ExecEnvironment.Exit(1);
                                        return;
                                    }
                                    process.WaitForExit();
                                    if (process.ExitCode != 0)
                                    {
                                        RedirectableConsole.ErrorWriteLine(
                                            "ERROR: Non-zero exit code " + process.ExitCode);
                                        ExecEnvironment.Exit(1);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // If this is not a normal project at this point, don't process it.
            if (projectDoc.DocumentElement.Name != "Project")
                return;

            // Load the appropriate project transformation XSLT.
            var languageAttribute = projectDoc.DocumentElement.Attributes["Language"];
            var languageText = languageAttribute != null ? languageAttribute.Value : "C#";
            var language = this.m_LanguageStringProvider.GetLanguageFromConfigurationName(languageText);
            var projectTransform = this.m_ResourceProvider.LoadXSLT(workingDirectory, ResourceType.GenerateProject, language, platformName);

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

			// Add include projects if they have an AppliesTo tag that matches this project's name.
            this._includeProjectAppliesToUpdater.UpdateProjectReferences(definitions.Select(x => x.Project).ToList(), projectDoc);

            // Add references and properties from include projects.
            _includeProjectMerger.MergeInReferencesAndPropertiesForIncludeProjects(definitions, projectDoc, platformName);

            // Imply external project references from other external projects.  We do
            // this so that external projects can reference other external projects (which
            // we can't reasonably handle at the XSLT level since it's recursive).
            this.m_ExternalProjectReferenceResolver.ResolveExternalProjectReferences(definitions, projectDoc, platformName);

            // Generate Info.plist files if necessary (for Mac / iOS).
            this._mPlatformResourcesGenerator.GenerateInfoPListIfNeeded(definitions, current, projectDoc, platformName);

            // Work out what path to save at.
            var path = Path.Combine(
                rootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar),
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                platformName + "." + this.m_LanguageStringProvider.GetProjectExtension(language));

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
                definitions.Select(x => x.Project).ToList(),
                workingDirectory,
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

            var settings = new XmlWriterSettings();
            settings.Indent = true;

            if (debugProjectGeneration)
            {
                using (var writer = XmlWriter.Create(path + ".input", settings))
                {
                    input.Save(writer);
                }
            }

            // Transform the input document using the XSLT transform.
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

