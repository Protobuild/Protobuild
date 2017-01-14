using System;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Protobuild.Services;

namespace Protobuild
{
    internal class NuGetAutomaticModulePackager : IAutomaticModulePackager
    {
        private readonly IProjectLoader m_ProjectLoader;

        private readonly IServiceInputGenerator m_ServiceInputGenerator;

        private readonly IProjectOutputPathCalculator m_ProjectOutputPathCalculator;

        private readonly INuGetPlatformMapping _nuGetPlatformMapping;

        public NuGetAutomaticModulePackager(
            IProjectLoader projectLoader,
            IServiceInputGenerator serviceInputGenerator,
            IProjectOutputPathCalculator projectOutputPathCalculator,
            INuGetPlatformMapping nuGetPlatformMapping)
        {
            this.m_ProjectLoader = projectLoader;
            this.m_ServiceInputGenerator = serviceInputGenerator;
            this.m_ProjectOutputPathCalculator = projectOutputPathCalculator;
            _nuGetPlatformMapping = nuGetPlatformMapping;
        }

        public void Autopackage(
            FileFilter fileFilter,
            Execution execution,
            ModuleInfo module,
            string rootPath,
            string platform,
            string packageFormat,
            List<string> temporaryFiles)
        {
            if (string.Equals(platform, "Unified", StringComparison.InvariantCulture))
            {
                AutopackageUnified(
                    fileFilter,
                    execution,
                    module,
                    rootPath,
                    platform,
                    packageFormat,
                    temporaryFiles);
            }
            else if (string.Equals(platform, "Template", StringComparison.InvariantCulture) || execution.PackageType == PackageManager.PACKAGE_TYPE_TEMPLATE)
            {
                AutopackageTemplate(
                    fileFilter,
                    execution,
                    module,
                    rootPath,
                    "Template",
                    packageFormat,
                    temporaryFiles);
            }
            else
            {
                AutopackagePlatform(
                    fileFilter,
                    execution,
                    module,
                    rootPath,
                    platform,
                    packageFormat,
                    temporaryFiles);
            }
        }

        private void AutopackageUnified(
            FileFilter fileFilter,
            Execution execution,
            ModuleInfo module,
            string rootPath,
            string platform,
            string packageFormat,
            List<string> temporaryFiles)
        {
            // We are going to combine all the nupkg files in the current module folder
            // into one unified package.  We only use package files that target exactly
            // one platform when performing this operation.
            RedirectableConsole.WriteLine("NuGet: Creating unified platform package...");

            string[] supportedPlatforms = null;
            if (!string.IsNullOrWhiteSpace(module.SupportedPlatforms))
            {
                supportedPlatforms = module.SupportedPlatforms.Split(new[] {','},
                    StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
            }

            var dedupIndex = new Dictionary<string, string>();

            var processedPlatforms = new List<string>();
            var packageFiles = new DirectoryInfo(module.Path).GetFiles("*.nupkg");
            foreach (var file in packageFiles)
            {
                using (var storer = ZipStorer.Open(file.FullName, FileAccess.Read))
                {
                    var entries = storer.ReadCentralDir();

                    var metadataEntries = entries.Where(x => x.FilenameInZip == "Package.xml").ToList();
                    if (metadataEntries.Count == 0)
                    {
                        RedirectableConsole.ErrorWriteLine("NuGet: Skipping package " + file.Name + " because it has no Package.xml file");
                        continue;
                    }

                    var metadataEntry = metadataEntries[0];

                    XmlDocument document;
                    using (var memory = new MemoryStream())
                    {
                        storer.ExtractFile(metadataEntry, memory);
                        memory.Seek(0, SeekOrigin.Begin);
                        document = new XmlDocument();
                        document.Load(memory);
                    }

                    var platforms = document.SelectNodes("/Package/BinaryPlatforms/Platform");
                    if (platforms == null || platforms.Count == 0)
                    {
                        RedirectableConsole.ErrorWriteLine("NuGet: Skipping package " + file.Name + " because it contains no binary platforms");
                        continue;
                    }
                    if (platforms.Count > 1)
                    {
                        RedirectableConsole.ErrorWriteLine("NuGet: Skipping package " + file.Name + " because it contains more than one binary platform");
                        continue;
                    }

                    var binaryPlatform = platforms[0].InnerText;
                    if (processedPlatforms.Contains(binaryPlatform))
                    {
                        RedirectableConsole.ErrorWriteLine("NuGet: Skipping package " + file.Name + " because a package for the '" + binaryPlatform + "' platform has already been processed");
                        continue;
                    }
                    if (supportedPlatforms != null)
                    {
                        if (!supportedPlatforms.Contains(binaryPlatform))
                        {
                            RedirectableConsole.ErrorWriteLine("NuGet: Skipping package " + file.Name + " because the '" + binaryPlatform + "' platform is not supported by this module");
                            continue;
                        }
                    }

                    var tempFile = Path.GetTempFileName();
                    File.Delete(tempFile);
                    Directory.CreateDirectory(tempFile);

                    var addedEntries = new HashSet<string>();

                    foreach (var entry in entries)
                    {
                        if (entry.FilenameInZip == "Package.xml")
                        {
                            continue;
                        }

                        if (entry.FilenameInZip == "_DedupIndex.txt")
                        {
                            continue;
                        }

                        var extractedPath = Path.Combine(tempFile, entry.FilenameInZip);
                        storer.ExtractFile(entry, extractedPath);
                        temporaryFiles.Add(extractedPath);

                        if (!addedEntries.Contains(entry.FilenameInZip))
                        {
                            fileFilter.AddManualMapping(extractedPath, entry.FilenameInZip);
                            addedEntries.Add(entry.FilenameInZip);
                        }
                    }

                    var dedupEntries = entries.Where(x => x.FilenameInZip == "_DedupIndex.txt").ToList();
                    if (dedupEntries.Count != 0)
                    {
                        var dedupEntry = dedupEntries[0];
                        var memory = new MemoryStream();
                        storer.ExtractFile(dedupEntry, memory);
                        memory.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(memory))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                var components = line.Split(new[] { '?' }, 2);
                                if (components.Length == 2 && (components.Length >= 1 && !string.IsNullOrWhiteSpace(components[0])))
                                {
                                    if (!dedupIndex.ContainsKey(components[0]))
                                    {
                                        dedupIndex[components[0]] = components[1];
                                    }
                                }
                            }
                        }
                    }

                    processedPlatforms.Add(binaryPlatform);
                }
            }

            if (dedupIndex.Count > 0)
            {
                var name = "_DedupIndex.txt";
                var temp = Path.Combine(Path.GetTempPath(), name);
                temporaryFiles.Add(temp);
                using (var writer = new StreamWriter(temp))
                {
                    foreach (var kv in dedupIndex)
                    {
                        writer.WriteLine(kv.Key + "?" + kv.Value);
                    }
                }
                fileFilter.AddManualMapping(temp, "_DedupIndex.txt");
            }

            if (processedPlatforms.Count == 0)
            {
                RedirectableConsole.ErrorWriteLine(
                    "NuGet: No other .nupkg files were valid candidates for creating a unified package.");
                throw new InvalidOperationException("NuGet: No other .nupkg files were valid candidates for creating a unified package.");
            }
            else
            {
                AddNuGetContentTypes(fileFilter, temporaryFiles);
                AddNuGetRelationships(module, fileFilter, temporaryFiles);
                AddNuGetSpecification(module, fileFilter, temporaryFiles, processedPlatforms.ToArray(), execution);
                AddPackageMetadata(module, fileFilter, temporaryFiles, processedPlatforms.ToArray(), execution);
            }
        }

        private void AutopackageTemplate(
            FileFilter fileFilter,
            Execution execution,
            ModuleInfo module,
            string rootPath,
            string platform,
            string packageFormat,
            List<string> temporaryFiles)
        {
            fileFilter.ApplyInclude("^.*$");
            fileFilter.ApplyExclude("^\\.git/.*$");
            fileFilter.ApplyExclude("^\\.hg/.*$");
            fileFilter.ApplyExclude("^\\.svn/.*$");
            fileFilter.ApplyExclude("^_TemplateOnly/.*$");
            fileFilter.ApplyExclude("^Jenkinsfile$");
            fileFilter.ApplyExclude("^automated\\.build$");
            fileFilter.ApplyExclude("^[Pp]rotobuild\\.exe$");
            fileFilter.ApplyExclude("^(.*)\\.nupkg$");
            fileFilter.ApplyExclude("^(.*)\\.nuspec$");
            fileFilter.ApplyExclude("^Build/Module\\.xml$");

            // Exclude any folders that are from packages.
            foreach (var package in module.Packages)
            {
                fileFilter.ApplyExclude("^" + Regex.Escape(package.Folder) + "/(.*)$");
            }

            fileFilter.ApplyRewrite("^Build/Module\\.xml\\.template$", "Build/Module.xml");
            fileFilter.ApplyRewrite("^automated\\.build\\.template$", "automated.build");
            fileFilter.ApplyRewrite("^(.*)$", "protobuild/Template/$1");

            execution.PackageType = PackageManager.PACKAGE_TYPE_TEMPLATE;

            // Add required NuGet content.
            AddNuGetContentTypes(fileFilter, temporaryFiles);
            AddNuGetRelationships(module, fileFilter, temporaryFiles);
            AddNuGetSpecification(module, fileFilter, temporaryFiles, new[] { platform }, execution);
            AddPackageMetadata(module, fileFilter, temporaryFiles, new[] { platform }, execution);
        }

        private void AutopackagePlatform(
            FileFilter fileFilter,
            Execution execution,
            ModuleInfo module,
            string rootPath,
            string platform,
            string packageFormat,
            List<string> temporaryFiles)
        {
            var definitions = module.GetDefinitionsRecursively(platform).ToArray();
            var loadedProjects = new List<LoadedDefinitionInfo>();

            foreach (var definition in definitions)
            {
                RedirectableConsole.WriteLine("Loading: " + definition.Name);
                loadedProjects.Add(
                    this.m_ProjectLoader.Load(
                        platform,
                        module,
                        definition));
            }

            var serviceManager = new ServiceManager(platform);
            List<Service> services;

            serviceManager.SetRootDefinitions(module.GetDefinitions());

            var enabledServices = execution.EnabledServices.ToArray();
            var disabledServices = execution.DisabledServices.ToArray();

            foreach (var service in enabledServices)
            {
                serviceManager.EnableService(service);
            }

            foreach (var service in disabledServices)
            {
                serviceManager.DisableService(service);
            }

            if (execution.DebugServiceResolution)
            {
                serviceManager.EnableDebugInformation();
            }

            services = serviceManager.CalculateDependencyGraph(loadedProjects.Select(x => x.Project).ToList());

            foreach (var service in services)
            {
                if (service.ServiceName != null)
                {
                    RedirectableConsole.WriteLine("Enabled service: " + service.FullName);
                }
            }

            var packagePaths = module.Packages
                .Select(x => new DirectoryInfo(System.IO.Path.Combine(module.Path, x.Folder)).FullName)
                .ToArray();
            
            foreach (var definition in definitions)
            {
                if (definition.SkipAutopackage)
                {
                    RedirectableConsole.WriteLine("Skipping: " + definition.Name);
                    continue;
                }

                var definitionNormalizedPath = new FileInfo(definition.AbsolutePath).FullName;
                if (packagePaths.Any(definitionNormalizedPath.StartsWith))
                {
                    RedirectableConsole.WriteLine("Skipping: " + definition.Name + " (part of another package)");
                    continue;
                }

                switch (definition.Type)
                {
                    case "External":
                        RedirectableConsole.WriteLine("Packaging: " + definition.Name);
                        this.AutomaticallyPackageExternalProject(definitions, services, fileFilter, rootPath, platform, definition, temporaryFiles);
                        break;
                    case "Include":
                        RedirectableConsole.WriteLine("Packaging: " + definition.Name);
                        this.AutomaticallyPackageIncludeProject(definitions, services, fileFilter, rootPath, platform, definition);
                        break;
                    case "Content":
                        RedirectableConsole.WriteLine("Packaging: " + definition.Name);
                        this.AutomaticallyPackageContentProject(definitions, services, fileFilter, rootPath, platform, definition);
                        break;
                    default:
                        RedirectableConsole.WriteLine("Packaging: " + definition.Name);
                        this.AutomaticallyPackageNormalProject(definitions, services, fileFilter, rootPath, platform, definition, temporaryFiles);
                        break;
                }
            }

            // If there is no Module.xml in the source mappings already, then copy the current module.
            if (!fileFilter.ContainsTargetPath("Build/Module.xml"))
            {
                fileFilter.AddManualMapping(Path.Combine(module.Path, "Build", "Module.xml"), "protobuild/" + platform + "/Build/Module.xml");
            }

            // Add required NuGet content.
            AddNuGetContentTypes(fileFilter, temporaryFiles);
            AddNuGetRelationships(module, fileFilter, temporaryFiles);
            AddNuGetSpecification(module, fileFilter, temporaryFiles, new[] { platform }, execution);
            AddPackageMetadata(module, fileFilter, temporaryFiles, new[] { platform }, execution);
        }

        private void AddPackageMetadata(ModuleInfo module, FileFilter fileFilter, List<string> temporaryFiles, string[] platforms, Execution execution)
        {
            RedirectableConsole.WriteLine("Protobuild: Generating Package.xml...");
            var name = "Package.xml";
            var temp = Path.Combine(Path.GetTempPath(), name);
            temporaryFiles.Add(temp);

            var document = new XmlDocument();
            document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));

            var packageElem = document.CreateElement("Package");
            document.AppendChild(packageElem);

            var binaryPlatforms = document.CreateElement("BinaryPlatforms");
            packageElem.AppendChild(binaryPlatforms);

            foreach (var platform in platforms)
            {
                var platformElem = document.CreateElement("Platform");
                binaryPlatforms.AppendChild(platformElem);

                platformElem.InnerText = platform;
            }

            var sourceElem = document.CreateElement("Source");
            packageElem.AppendChild(sourceElem);

            if (!string.IsNullOrWhiteSpace(execution.PackageGitCommit))
            {
                var gitCommitElem = document.CreateElement("GitCommitHash");
                gitCommitElem.InnerText = execution.PackageGitCommit;
                sourceElem.AppendChild(gitCommitElem);
            }

            if (!string.IsNullOrWhiteSpace(execution.PackageGitRepositoryUrl))
            {
                var gitRepoElem = document.CreateElement("GitRepositoryUrl");
                gitRepoElem.InnerText = execution.PackageGitRepositoryUrl;
                sourceElem.AppendChild(gitRepoElem);
            }
            else if (!string.IsNullOrWhiteSpace(module.GitRepositoryUrl))
            {
                var gitRepoElem = document.CreateElement("GitRepositoryUrl");
                gitRepoElem.InnerText = module.GitRepositoryUrl;
                sourceElem.AppendChild(gitRepoElem);
            }

            using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true, IndentChars = "  " }))
            {
                document.WriteTo(writer);
            }

            fileFilter.AddManualMapping(temp, "Package.xml");
        }

        private void AddNuGetContentTypes(FileFilter fileFilter, List<string> temporaryFiles)
        {
            const string contentTypeBlob =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" /><Default Extension=\"nuspec\" ContentType=\"application/octet\" /><Default Extension=\"dll\" ContentType=\"application/octet\" /><Default Extension=\"psmdcp\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\" /></Types>";
            RedirectableConsole.WriteLine("NuGet: Generating [Content_Types].xml...");
            var name = "[Content_Types].xml";
            var temp = Path.Combine(Path.GetTempPath(), name);
            temporaryFiles.Add(temp);
            using (var writer = new StreamWriter(temp))
            {
                writer.Write(contentTypeBlob);
            }
            fileFilter.AddManualMapping(temp, "[Content_Types].xml");
        }

        private void AddNuGetRelationships(ModuleInfo module, FileFilter fileFilter, List<string> temporaryFiles)
        {
            var relationshipsBlob =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Type=\"http://schemas.microsoft.com/packaging/2010/07/manifest\" Target=\"/" + module.Name + ".nuspec\" Id=\"Rfb095b1884c14816\" /><Relationship Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"/package/services/metadata/core-properties/3809d589ed3d4d4bb90bd9966a1fe2c5.psmdcp\" Id=\"R0e5b6d887aca4fd9\" /></Relationships>";
            RedirectableConsole.WriteLine("NuGet: Generating _rels/.rels...");
            var name = ".rels.xml";
            var temp = Path.Combine(Path.GetTempPath(), name);
            temporaryFiles.Add(temp);
            using (var writer = new StreamWriter(temp))
            {
                writer.Write(relationshipsBlob);
            }
            fileFilter.AddManualMapping(temp, "_rels/.rels");
        }

        private void AddNuGetSpecification(ModuleInfo module, FileFilter fileFilter, List<string> temporaryFiles, string[] platforms, Execution execution)
        {
            RedirectableConsole.WriteLine("NuGet: Generating " + module.Name + ".nuspec...");

            const string specPrefix =
                "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";

            var document = new XmlDocument();
            document.AppendChild(
                document.CreateXmlDeclaration("1.0", null, null));

            var package = document.CreateElement(null, "package", specPrefix);
            document.AppendChild(package);

            var metadata = document.CreateElement(null, "metadata", specPrefix);
            package.AppendChild(metadata);

            var id = document.CreateElement(null, "id", specPrefix);
            var version = document.CreateElement(null, "version", specPrefix);
            var title = document.CreateElement(null, "title", specPrefix);
            var authors = document.CreateElement(null, "authors", specPrefix);
            var licenseUrl = document.CreateElement(null, "licenseUrl", specPrefix);
            var projectUrl = document.CreateElement(null, "projectUrl", specPrefix);
            var iconUrl = document.CreateElement(null, "iconUrl", specPrefix);
            var requireLicenseAcceptance = document.CreateElement(null, "requireLicenseAcceptance", specPrefix);
            var description = document.CreateElement(null, "description", specPrefix);
            var tags = document.CreateElement(null, "tags", specPrefix);

            var tagsList = new List<string>();
            tagsList.Add("platforms=" + string.Join(",", platforms ?? new string[0]));
            if (!string.IsNullOrWhiteSpace(execution.PackageType))
            {
                tagsList.Add("type=" + execution.PackageType);
            }
            else
            {
                tagsList.Add("type=" + PackageManager.PACKAGE_TYPE_LIBRARY);
            }
            if (!string.IsNullOrWhiteSpace(execution.PackageGitCommit))
            {
                tagsList.Add("commit=" + execution.PackageGitCommit);
            }
            if (!string.IsNullOrWhiteSpace(execution.PackageGitRepositoryUrl))
            {
                tagsList.Add("git=" + execution.PackageGitRepositoryUrl);
            }
            else if (!string.IsNullOrWhiteSpace(module.GitRepositoryUrl))
            {
                tagsList.Add("git=" + module.GitRepositoryUrl);
            }

            id.InnerText = module.Name;
            version.InnerText = GenerateNuGetSemanticVersion(module);
            title.InnerText = module.Name;
            authors.InnerText = module.Authors ?? "No Authors Specified";
            licenseUrl.InnerText = module.LicenseUrl;
            projectUrl.InnerText = module.ProjectUrl;
            iconUrl.InnerText = module.IconUrl;
            requireLicenseAcceptance.InnerText = "false";
            description.InnerText = module.Description ?? "No Description Specified";
            tags.InnerText = string.Join(" ", tagsList);

            metadata.AppendChild(id);
            metadata.AppendChild(version);
            metadata.AppendChild(title);
            metadata.AppendChild(authors);
            if (!string.IsNullOrWhiteSpace(module.LicenseUrl))
            {
                metadata.AppendChild(licenseUrl);
            }
            if (!string.IsNullOrWhiteSpace(module.ProjectUrl))
            {
                metadata.AppendChild(projectUrl);
            }
            if (!string.IsNullOrWhiteSpace(module.IconUrl))
            {
                metadata.AppendChild(iconUrl);
            }
            metadata.AppendChild(requireLicenseAcceptance);
            metadata.AppendChild(description);
            metadata.AppendChild(tags);

            var name = module.Name + ".nuspec";
            var temp = Path.Combine(Path.GetTempPath(), name);
            temporaryFiles.Add(temp);
            using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true, IndentChars = "  " }))
            {
                document.WriteTo(writer);
            }
            fileFilter.AddManualMapping(temp, module.Name + ".nuspec");
        }

        private string GenerateNuGetSemanticVersion(ModuleInfo module)
        {
            if (module.SemanticVersion != null)
            {
                return module.SemanticVersion;
            }

            var utcTime = DateTime.UtcNow;
            var major = utcTime.Year.ToString("D", CultureInfo.InvariantCulture).Substring(1).TrimStart(new[] { '0' });
            var minor =
                utcTime.Month.ToString("D2", CultureInfo.InvariantCulture) +
                utcTime.Day.ToString("D2", CultureInfo.InvariantCulture);
            var patch =
                utcTime.Hour.ToString("D2", CultureInfo.InvariantCulture) +
                utcTime.Minute.ToString("D2", CultureInfo.InvariantCulture) +
                utcTime.Second.ToString("D2", CultureInfo.InvariantCulture);
            return major + "." + minor + "." + patch;
        }

        private void AutomaticallyPackageExternalProject(
            DefinitionInfo[] definitions, 
            List<Service> services, 
            FileFilter fileFilter, 
            string rootPath,
            string platform, 
            DefinitionInfo definition,
            List<string> temporaryFiles)
        {
            var document = new XmlDocument();
            document.Load(definition.DefinitionPath);

            var externalProjectDocument = new XmlDocument();
            externalProjectDocument.AppendChild(externalProjectDocument.CreateXmlDeclaration("1.0", "UTF-8", null));
            var externalProject = externalProjectDocument.CreateElement("ExternalProject");
            externalProjectDocument.AppendChild(externalProject);
            externalProject.SetAttribute("Name", definition.Name);

            // Translate all of the references (this is a seperate method so we can call it
            // again when traversing into Platform nodes).
            this.TranslateExternalProject(
                definition,
                services, 
                fileFilter, 
                rootPath,
                platform,
                document.DocumentElement, 
                externalProject,
                false);

            // Write out the external project to a temporary file and include it.
            var name = Path.GetRandomFileName() + "_" + definition.Name + ".xml";
            var temp = Path.Combine(Path.GetTempPath(), name);
            temporaryFiles.Add(temp);
            using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true, IndentChars = "  " }))
            {
                externalProjectDocument.WriteTo(writer);
            }
            fileFilter.AddManualMapping(temp, "protobuild/" + platform + "/Build/Projects/" + definition.Name + ".definition");
        }

        private void AutomaticallyPackageContentProject(
            DefinitionInfo[] definitions,
            List<Service> services,
            FileFilter fileFilter,
            string rootPath,
            string platform,
            DefinitionInfo definition)
        {
            var document = new XmlDocument();
            document.Load(definition.DefinitionPath);

            // Copy the definition file as-is.
            fileFilter.ApplyInclude("^" + Regex.Escape("Build/Projects/" + definition.Name + ".definition") + "$");
            fileFilter.ApplyRewrite("^" + Regex.Escape("Build/Projects/" + definition.Name + ".definition") + "$", "protobuild/" + platform + "/Build/Projects/" + definition.Name + ".definition");

            // Load the content project and include all relevant paths.
            foreach (var element in document.SelectNodes("//Source").OfType<XmlElement>())
            {
                var include = element.GetAttribute("Include");
                var match = element.GetAttribute("Match");

                include = include.Replace("$(Platform)", platform);

                // TODO: Only include files that match?
                fileFilter.ApplyInclude("^" + Regex.Escape(include.TrimEnd(new[] { '/', '\\' })) + "/(.*)$");
                fileFilter.ApplyRewrite("^" + Regex.Escape(include.TrimEnd(new[] { '/', '\\' })) + "/(.*)$", "protobuild/" + platform + "/" + include.TrimEnd(new[] { '/', '\\' }) + "/$1");
            }
        }

        private void TranslateExternalProject(
            DefinitionInfo definition,
            List<Service> services, 
            FileFilter fileFilter, 
            string rootPath,
            string platform,
            XmlElement fromNode, 
            XmlElement toNode, 
            bool isNested)
        {
            foreach (var child in fromNode.ChildNodes.OfType<XmlElement>())
            {
                switch (child.LocalName)
                {
                    case "Reference":
                        {
                            // These are included as-is; either they're references to GAC assemblies
                            // or other Protobuild projects (which are now external projects themselves).
                            var referenceEntry = toNode.OwnerDocument.ImportNode(child, true);
                            toNode.AppendChild(referenceEntry);
                            break;
                        }
                    case "Binary":
                        {
                            // We have to change the path on these files so that the path is nested
                            // underneath the "_AutomaticExternals" folder.  Since these are .NET references, we also
                            // want to copy all of the related files (config, xml, etc.)
                            var sourceFileInfo = new FileInfo(Path.Combine(definition.ModulePath, child.GetAttribute("Path")));
                            var sourceWithoutExtension = sourceFileInfo.FullName.Substring(
                                0, 
                                sourceFileInfo.FullName.Length - sourceFileInfo.Extension.Length);
                            var destWithoutExtension = child.GetAttribute("Path").Substring(
                                0, 
                                child.GetAttribute("Path").Length - sourceFileInfo.Extension.Length);

                            var fileExtensionsToCopy = new[]
                            {
                                sourceFileInfo.Extension,
                                sourceFileInfo.Extension + ".config",
                                sourceFileInfo.Extension + ".mdb",
                                ".pdb",
                                ".xml",
                            };

                            foreach (var extension in fileExtensionsToCopy)
                            {
                                var sourcePath = sourceWithoutExtension + extension;
                                sourcePath = sourcePath.Substring(rootPath.Length).Replace('\\', '/').TrimStart('/');
                                var destPath = Path.Combine("_AutomaticExternals", destWithoutExtension + extension);

                                var sourcePathRegex = this.ConvertPathWithMSBuildVariablesFind(sourcePath);
                                var destPathRegex = this.ConvertPathWithMSBuildVariablesReplace(destPath.Replace('\\', '/'));

                                var includeMatch = fileFilter.ApplyInclude(sourcePathRegex);
                                if (sourceFileInfo.Extension == ".exe")
                                {
                                    fileFilter.ApplyCopy(sourcePathRegex, "tools/" + destPathRegex);
                                }
                                else
                                {
                                    fileFilter.ApplyCopy(sourcePathRegex, "lib/" + _nuGetPlatformMapping.GetFrameworkNameForWrite(platform) + "/" + destPathRegex);
                                }
                                fileFilter.ApplyRewrite(sourcePathRegex, "protobuild/" + platform + "/" + destPathRegex);
                                if (includeMatch)
                                {
                                    if (extension == sourceFileInfo.Extension)
                                    {
                                        var binaryEntry = toNode.OwnerDocument.CreateElement("Binary");
                                        binaryEntry.SetAttribute("Name", child.GetAttribute("Name"));
                                        binaryEntry.SetAttribute("Path", destPath);
                                        toNode.AppendChild(binaryEntry);
                                    }
                                    else if (extension == sourceFileInfo.Extension + ".config")
                                    {
                                        var nativeBinaryEntry = toNode.OwnerDocument.CreateElement("NativeBinary");
                                        nativeBinaryEntry.SetAttribute("Path", destPath);
                                        toNode.AppendChild(nativeBinaryEntry);
                                    }
                                }
                            }

                            break;
                        }
                    case "NativeBinary":
                        {
                            // We have to change the path on these files so that the path is nested
                            // underneath the "_AutomaticExternals" folder.
                            var sourcePath = Path.Combine(definition.ModulePath, child.GetAttribute("Path"));
                            sourcePath = sourcePath.Substring(rootPath.Length).Replace('\\', '/').TrimStart('/');
                            var destPath = Path.Combine("_AutomaticExternals", child.GetAttribute("Path"));

                            var sourcePathRegex = this.ConvertPathWithMSBuildVariablesFind(sourcePath);
                            var destPathRegex = this.ConvertPathWithMSBuildVariablesReplace(destPath.Replace('\\', '/'));

                            var includeMatch = fileFilter.ApplyInclude(sourcePathRegex);
                            fileFilter.ApplyRewrite(sourcePathRegex, "protobuild/" + platform + "/" + destPathRegex);
                            if (includeMatch)
                            {
                                var nativeBinaryEntry = toNode.OwnerDocument.CreateElement("NativeBinary");
                                nativeBinaryEntry.SetAttribute("Path", destPath);
                                toNode.AppendChild(nativeBinaryEntry);
                            }
                            else
                            {
                                throw new InvalidOperationException("File not found at " + sourcePath + " when converting NativeBinary reference.");
                            }

                            break;
                        }
                    case "Project":
                        {
                            // We can't do anything with these tags, as we don't know how the target C# project
                            // is built (without investing heavily into how MSBuild builds projects).  Instead,
                            // show a warning that this reference will be converted to an external project
                            // reference instead, so that the developer can manually hook this up through
                            // additional directives in the filter file.
                            RedirectableConsole.WriteLine(
                                "WARNING: The 'Project' tag in external projects can not be " + 
                                "automatically converted during packaging.  This reference " +
                                "to '" + child.GetAttribute("Name") + "' will be converted to refer " + 
                                "to an external reference with the name " +
                                "'" + child.GetAttribute("Name") + ".External' instead, and you'll " +
                                "need to write an ExternalProject definition (and include it via " +
                                "a filter file) to have this reference packaged correctly.");
                            var referenceEntry = toNode.OwnerDocument.CreateElement("Reference");
                            referenceEntry.SetAttribute("Include", child.GetAttribute("Name") + ".External");
                            toNode.AppendChild(referenceEntry);
                            break;
                        }
                    case "Platform":
                        {
                            // If the Platform tag matches, copy the contents of it directly into our top-level
                            // external project.  We don't need to copy the Platform tag itself, as packages are
                            // specific to a given platform.
                            if (child.GetAttribute("Type").ToLowerInvariant() == platform.ToLowerInvariant())
                            {
                                this.TranslateExternalProject(definition, services, fileFilter, rootPath, platform, child, toNode, true);
                            }

                            break;
                        }
                    case "Service":
                        {
                            // If the service is enabled, then copy the contents of the service section directly
                            // into our top-level external project.  We don't need to copy the Service tag itself,
                            // as changing services won't have any effect on binary packages.
                            var service = services.FirstOrDefault(x => 
                                x.FullName == child.GetAttribute("Name") ||
                                x.FullName == definition.Name + "/" + child.GetAttribute("Name"));
                            if (service != null)
                            {
                                // If the service is present in the list, then it is enabled.
                                this.TranslateExternalProject(definition, services, fileFilter, rootPath, platform, child, toNode, true);
                            }

                            break;
                        }
                    case "Tool":
                        {
                            // These are included as-is; they are only explicitly set when an external tool
                            // (that does not use Protobuild) is being packaged up.
                            var referenceEntry = toNode.OwnerDocument.ImportNode(child, true);
                            toNode.AppendChild(referenceEntry);
                            break;
                        }
                    default:
                        {
                            // We can't do anything with these tags, because we don't know what services were enabled
                            // when the assemblies were built.  Show a warning instead.
                            RedirectableConsole.WriteLine("WARNING: Unknown tag '" + child.LocalName + "' encountered.");
                            break;
                        }
                }
            }
        }

        private string ConvertPathWithMSBuildVariablesFind(string sourcePath)
        {
            var variableRegex = new Regex("\\$\\([^\\)]+\\)");
            var matches = variableRegex.Matches(sourcePath);
            var current = 0;
            var rawText = new List<string>();
            for (var i = 0; i < matches.Count; i++)
            {
                rawText.Add(sourcePath.Substring(current, matches[i].Index));
                current = matches[i].Index + matches[i].Length;
            }
            if (current < sourcePath.Length)
            {
                rawText.Add(sourcePath.Substring(current));
            }
            sourcePath = string.Join("([^/]+)", rawText.Select(x => Regex.Escape(x)));
            return "^" + sourcePath + "$";
        }

        private string ConvertPathWithMSBuildVariablesReplace(string destPath)
        {
            var variableRegex = new Regex("\\$\\([^\\)]+\\)");
            var matches = variableRegex.Matches(destPath);
            var current = 0;
            var rawText = new List<string>();
            for (var i = 0; i < matches.Count; i++)
            {
                rawText.Add(destPath.Substring(current, matches[i].Index));
                current = matches[i].Index + matches[i].Length;
            }
            if (current < destPath.Length)
            {
                rawText.Add(destPath.Substring(current));
            }
            var escapedText = rawText.Select(x => x.Replace("$", "$$")).ToList();
            destPath = string.Empty;
            for (var i = 0; i < escapedText.Count; i++)
            {
                if (i > 0)
                {
                    destPath += "$" + i;
                }

                destPath += escapedText[i];
            }
            return destPath;
        }

        private void AutomaticallyPackageNormalProject(
            DefinitionInfo[] definitions,
            List<Service> services,  
            FileFilter fileFilter, 
            string rootPath,
            string platform,
            DefinitionInfo definition,
            List<string> temporaryFiles)
        {
            var document = XDocument.Load(definition.DefinitionPath);

            var externalProjectDocument = new XmlDocument();
            externalProjectDocument.AppendChild(externalProjectDocument.CreateXmlDeclaration("1.0", "UTF-8", null));
            var externalProject = externalProjectDocument.CreateElement("ExternalProject");
            externalProjectDocument.AppendChild(externalProject);
            externalProject.SetAttribute("Name", definition.Name);

            if (definition.PostBuildHook)
            {
                externalProject.SetAttribute("PostBuildHook", "True");
            }

            var externalProjectServices = externalProjectDocument.CreateElement("Services");
            externalProject.AppendChild(externalProjectServices);

            // Just import all declared services as available, regardless of conflicts or
            // requirements.  We don't have a clean way of automatically translating
            // services for packages (it has to be done manually because services can
            // change the resulting code that's built).  So that things work 95% of time,
            // just declare all services available for projects included via the automatic
            // packaging mechanism.
            var servicesToDeclare = new List<string>();
            var servicesDeclared = document.Root.Element(XName.Get("Services"));
            if (servicesDeclared != null)
            {
                foreach (var serviceElement in servicesDeclared.Elements().Where(x => x.Name.LocalName == "Service"))
                {
                    servicesToDeclare.Add(serviceElement.Attribute(XName.Get("Name")).Value);
                }
            }

            foreach (var serviceToDeclare in servicesToDeclare)
            {
                var serviceElem = externalProjectDocument.CreateElement("Service");
                serviceElem.SetAttribute("Name", serviceToDeclare);
                var defaultForRoot = externalProjectDocument.CreateElement("DefaultForRoot");
                defaultForRoot.InnerText = "True";
                serviceElem.AppendChild(defaultForRoot);
                externalProjectServices.AppendChild(serviceElem);
            }

            var pathPrefix = this.m_ProjectOutputPathCalculator.GetProjectOutputPathPrefix(platform, definition, document, true);
            var assemblyName = this.m_ProjectOutputPathCalculator.GetProjectAssemblyName(platform, definition, document);
            var outputMode = this.m_ProjectOutputPathCalculator.GetProjectOutputMode(document);

            var assemblyFilesToCopy = new[]
            {
                assemblyName + ".exe",
                assemblyName + ".dll",
                assemblyName + ".dll.config",
                assemblyName + ".dll.mdb",
                assemblyName + ".pdb",
                assemblyName + ".xml",
            };

            // Copy the assembly itself out to the package.
            switch (outputMode)
            {
                case OutputPathMode.BinConfiguration:
                    {
                        // In this configuration, we only ship the binaries for
                        // the default architecture (because that's all we know
                        // about).  We also have to assume the binary folder
                        // contains binaries for the desired platform.
                        if (definition.Type == "Library")
                        {
                            // For libraries, we only copy the assembly (and immediately related files)
                            // into it's directory. Native binaries will be expressed through the ExternalProject.
                            foreach (var assemblyFile in assemblyFilesToCopy)
                            {
                                var includeMatch = fileFilter.ApplyInclude("^" + pathPrefix + Regex.Escape(assemblyFile) + "$");
                                fileFilter.ApplyCopy("^" + pathPrefix + Regex.Escape(assemblyFile) + "$", "lib/" + _nuGetPlatformMapping.GetFrameworkNameForWrite(platform) + "/" + assemblyFile);
                                var rewriteMatch = fileFilter.ApplyRewrite("^" + pathPrefix + Regex.Escape(assemblyFile) + "$", "protobuild/" + platform + "/" + definition.Name + "/AnyCPU/" + assemblyFile);
                                if (includeMatch && rewriteMatch)
                                {
                                    if (assemblyFile.EndsWith(".dll"))
                                    {
                                        var binaryEntry = externalProjectDocument.CreateElement("Binary");
                                        binaryEntry.SetAttribute("Name", assemblyFile.Substring(0, assemblyFile.Length - 4));
                                        binaryEntry.SetAttribute("Path", definition.Name + "\\AnyCPU\\" + assemblyFile);
                                        externalProject.AppendChild(binaryEntry);
                                    }
                                    else if (assemblyFile.EndsWith(".dll.config"))
                                    {
                                        var configEntry = externalProjectDocument.CreateElement("NativeBinary");
                                        configEntry.SetAttribute("Path", definition.Name + "\\AnyCPU\\" + assemblyFile);
                                        externalProject.AppendChild(configEntry);
                                    }
                                }
                                else if (includeMatch || rewriteMatch)
                                {
                                    throw new InvalidOperationException("Automatic filter; only one rule matched.");
                                }
                            }
                        }
                        else
                        {
                            // For executables, we ship everything in the output directory, because we
                            // want the executables to be able to run from the package directory.
                            fileFilter.ApplyInclude("^" + pathPrefix + "(.+)$");
                            
                            // Exclude the .app folder for Mac executables, because these applications are huge (they contain
                            // a Mono runtime), and they're almost certainly redundant for tools in packages, which are going
                            // to be used in development environments where the Mono runtime is available.
                            fileFilter.ApplyExclude("^" + pathPrefix + "(.+)\\.app/(.+)$");

                            // Copy remaining files to the right locations.
                            fileFilter.ApplyCopy("^" + pathPrefix + "(.+)$", "tools/$2");
                            fileFilter.ApplyRewrite("^" + pathPrefix + "(.+)$", "protobuild/" + platform + "/" + definition.Name + "/AnyCPU/$2");

                            // Mark the executable files in the directory as tools that can be executed.
                            foreach (var assemblyFile in assemblyFilesToCopy)
                            {
                                if (assemblyFile.EndsWith(".exe"))
                                {
                                    var binaryEntry = externalProjectDocument.CreateElement("Tool");
                                    binaryEntry.SetAttribute("Name", assemblyFile.Substring(0, assemblyFile.Length - 4));
                                    binaryEntry.SetAttribute("Path", definition.Name + "\\AnyCPU\\" + assemblyFile);
                                    externalProject.AppendChild(binaryEntry);
                                }
                            }
                        }

                        break;
                    }
                case OutputPathMode.BinPlatformArchConfiguration:
                    {
                        // In this configuration, we ship binaries for AnyCPU, iPhoneSimulator or all .NET architectures
                        // depending on whether or not the platform produces multiple architectures.  On Mono,
                        // we can't use $(Platform) within a reference's path, so we have to keep this path static
                        // for Mono platforms.
                        string pathArchMatch, pathArchReplace, pathArchRuntime;
                        switch (platform.ToLowerInvariant())
                        {
                            case "ios":
                                {
                                    pathArchMatch = "iPhoneSimulator";
									pathArchReplace = "iPhoneSimulator";
									pathArchRuntime = "iPhoneSimulator";
                                    break;
                                }
                            case "windowsphone":
                                {
                                    pathArchMatch = "([^/]+)";
                                    pathArchReplace = "$1";
                                    pathArchRuntime = "$(Platform)";
                                    break;
                                }
                            default:
                                {
                                    pathArchMatch = "AnyCPU";
                                    pathArchReplace = "AnyCPU";
                                    pathArchRuntime = "AnyCPU";
                                    break;
                                }
                        }

                        if (definition.Type == "Library")
                        {
                            // For libraries, we only copy the assembly (and immediately related files)
                            // into it's directory. Native binaries will be expressed through the ExternalProject.
                            foreach (var assemblyFile in assemblyFilesToCopy)
                            {
                                var includeMatch = fileFilter.ApplyInclude("^" + pathPrefix + Regex.Escape(assemblyFile) + "$");
                                fileFilter.ApplyCopy("^" + pathPrefix + Regex.Escape(assemblyFile) + "$", "lib/" + _nuGetPlatformMapping.GetFrameworkNameForWrite(platform) + "/" + assemblyFile);
                                var rewriteMatch = fileFilter.ApplyRewrite("^" + pathPrefix + Regex.Escape(assemblyFile) + "$", "protobuild/" + platform + "/" + definition.Name + "/" + pathArchReplace + "/" + assemblyFile);
                                if (includeMatch && rewriteMatch)
                                {
                                    if (assemblyFile.EndsWith(".dll"))
                                    {
                                        var binaryEntry = externalProjectDocument.CreateElement("Binary");
                                        binaryEntry.SetAttribute("Name", assemblyFile.Substring(0, assemblyFile.Length - 4));
                                        binaryEntry.SetAttribute("Path", definition.Name + "\\" + pathArchRuntime + "\\" + assemblyFile);
                                        externalProject.AppendChild(binaryEntry);
                                    }
                                    else if (assemblyFile.EndsWith(".dll.config"))
                                    {
                                        var configEntry = externalProjectDocument.CreateElement("NativeBinary");
                                        configEntry.SetAttribute("Path", definition.Name + "\\" + pathArchRuntime + "\\" + assemblyFile);
                                        externalProject.AppendChild(configEntry);
                                    }
                                }
                                else if (includeMatch || rewriteMatch)
                                {
                                    throw new InvalidOperationException("Automatic filter; only one rule matched.");
                                }
                            }
                        }
                        else
                        {
                            // For executables, we ship everything in the output directory, because we
                            // want the executables to be able to run from the package directory.
                            fileFilter.ApplyInclude("^" + pathPrefix + "(.+)$");

                            // Exclude the .app folder for Mac executables, because these applications are huge (they contain
                            // a Mono runtime), and they're almost certainly redundant for tools in packages, which are going
                            // to be used in development environments where the Mono runtime is available.
                            fileFilter.ApplyExclude("^" + pathPrefix + "(.+)\\.app/(.+)$");

                            // Copy remaining files to the right locations.
                            fileFilter.ApplyCopy("^" + pathPrefix + "(.+)$", "tools/$2");

                            if (pathArchMatch == "([^/]+)")
                            {
                                fileFilter.ApplyRewrite("^" + pathPrefix + "(.+)$", "protobuild/" + platform + "/" + definition.Name + "/" + pathArchReplace + "/$3");
                            }
                            else
                            {
                                fileFilter.ApplyRewrite("^" + pathPrefix + "(.+)$", "protobuild/" + platform + "/" + definition.Name + "/" + pathArchReplace + "/$2");
                            }
                            
                            // Mark the executable files in the directory as tools that can be executed.
                            foreach (var assemblyFile in assemblyFilesToCopy)
                            {
                                if (assemblyFile.EndsWith(".exe"))
                                {
                                    var binaryEntry = externalProjectDocument.CreateElement("Tool");
                                    binaryEntry.SetAttribute("Name", assemblyFile.Substring(0, assemblyFile.Length - 4));
                                    binaryEntry.SetAttribute("Path", definition.Name + "\\" + pathArchRuntime + "\\" + assemblyFile);
                                    externalProject.AppendChild(binaryEntry);
                                }
                            }
                        }

                        break;
                    }
                case OutputPathMode.BinProjectPlatformArchConfiguration:
                    {
                        throw new NotSupportedException();
                        break;
                    }
            }

            // Convert all of the known references into references within the external project.
            var definitionsByName = definitions.ToDictionarySafe(
                k => k.Name,
                v => v,
                (dict, x) =>
                {
                    var existing = dict[x.Name];
                    var tried = x;

                    RedirectableConsole.WriteLine("WARNING: There is more than one project with the name " +
                                      x.Name + " (first project loaded from " + tried.AbsolutePath + ", " +
                                      "skipped loading second project from " + existing.AbsolutePath + ")");
                });
            foreach (var reference in document.XPathSelectElements("/Project/References/Reference"))
            {
                var includeAttribute = reference.Attribute(XName.Get("Include"));
                if (includeAttribute != null)
                {
                    if (definitionsByName.ContainsKey(includeAttribute.Value))
                    {
                        var targetDefinition = definitionsByName[includeAttribute.Value];

                        // If the targeted reference is an include project, skip it.
                        if (targetDefinition == null || targetDefinition.Type != "Include")
                        {
                            // This reference will be converted to an external project,
                            // so add a reference to it (in case it contains native binaries
                            // which need to be copied out).
                            var referenceEntry = externalProjectDocument.CreateElement("Reference");
                            referenceEntry.SetAttribute("Include", includeAttribute.Value);
                            externalProject.AppendChild(referenceEntry);
                        }
                    }
                }
            }

            // Copy out any files that are marked with copy-on-build flag.
            var detector = new PlatformAndServiceActiveDetection();
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(definition.DefinitionPath);
            var servicesInput = this.m_ServiceInputGenerator.Generate(xmlDocument, definition.Name, services);
            var activeServicesElement = servicesInput.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.LocalName == "ActiveServicesNames");
            var activeServices = activeServicesElement.InnerText;
            foreach (var file in document.XPathSelectElements("/Project/Files/*"))
            {
                var copyOnBuild = file.XPathSelectElement("CopyToOutputDirectory");
                if (copyOnBuild == null)
                {
                    continue;
                }

                if (copyOnBuild.Value != "PreserveNewest" && copyOnBuild.Value != "Always")
                {
                    continue;
                }

                var platformsElement = file.XPathSelectElement("Platforms");
                var includePlatformsElement = file.XPathSelectElement("IncludePlatforms");
                var excludePlatformsElement = file.XPathSelectElement("ExcludePlatforms");
                var servicesElement = file.XPathSelectElement("Services");
                var includeServicesElement = file.XPathSelectElement("IncludeServices");
                var excludeServicesElement = file.XPathSelectElement("ExcludeServices");

                var platformsString = platformsElement != null ? platformsElement.Value : string.Empty;
                var includePlatformsString = includePlatformsElement != null ? includePlatformsElement.Value : string.Empty;
                var excludePlatformsString = excludePlatformsElement != null ? excludePlatformsElement.Value : string.Empty;
                var servicesString = servicesElement != null ? servicesElement.Value : string.Empty;
                var includeServicesString = includeServicesElement != null ? includeServicesElement.Value : string.Empty;
                var excludeServicesString = excludeServicesElement != null ? excludeServicesElement.Value : string.Empty;

                if (detector.ProjectAndServiceIsActive(
                    platformsString,
                    includePlatformsString,
                    excludePlatformsString,
                    servicesString,
                    includeServicesString,
                    excludeServicesString,
                    platform,
                    activeServices))
                {
                    var include = file.Attribute(XName.Get("Include"));
                    var linkElement = file.XPathSelectElement("Link");
                    var link = linkElement != null ? linkElement.Value : include.Value;

                    var fileInfo = new FileInfo(Path.Combine(
                        definition.AbsolutePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar), 
                        include.Value.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));

                    if (definition.Type == "Library")
                    {
                        // For libraries, we're copying the content so that applications using
                        // the libraries will have the content.  This is most often used to 
                        // ship native binaries (although NativeBinary in external projects now
                        // supersedes this).
                        if (link.Contains('/') || link.Contains('\\'))
                        {
                            RedirectableConsole.WriteLine(
                                "WARNING: Copy-on-build file '" + link + "' in library project which " +
                                "does not output to root of project detected.  This is not supported.");
                        }
                        else
                        {
                            if (fileInfo.Name != link)
                            {
                                RedirectableConsole.WriteLine(
                                    "WARNING: Copy-on-build file in library project does not have the same " +
                                    "name when copied to build directory.  This is not supported.");
                            }
                            else
                            {
                                var sourcePath = fileInfo.FullName;
                                sourcePath = sourcePath.Substring(rootPath.Length).Replace('\\', '/').TrimStart('/');
                                var destPath = Path.Combine("_AutomaticExternals", sourcePath);

                                var sourcePathRegex = this.ConvertPathWithMSBuildVariablesFind(sourcePath);
                                var destPathRegex = this.ConvertPathWithMSBuildVariablesReplace(destPath.Replace('\\', '/'));

                                var includeMatch = fileFilter.ApplyInclude(sourcePathRegex);
                                fileFilter.ApplyRewrite(sourcePathRegex, "protobuild/" + platform + "/" + destPathRegex);
                                if (includeMatch)
                                {
                                    var nativeBinaryEntry = externalProjectDocument.CreateElement("NativeBinary");
                                    nativeBinaryEntry.SetAttribute("Path", destPath);
                                    externalProject.AppendChild(nativeBinaryEntry);
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        "File not found at " + sourcePath + " when converting " +
                                        "copy-on-build file in library project.");
                                }
                            }
                        }
                    }
                }
            }

            // Write out the external project to a temporary file and include it.
            var name = Path.GetRandomFileName() + "_" + definition.Name + ".xml";
            var temp = Path.Combine(Path.GetTempPath(), name);
            temporaryFiles.Add(temp);
            using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true, IndentChars = "  " }))
            {
                externalProjectDocument.WriteTo(writer);
            }
            fileFilter.AddManualMapping(temp, "protobuild/" + platform + "/Build/Projects/" + definition.Name + ".definition");
        }

        private void AutomaticallyPackageIncludeProject(
            DefinitionInfo[] definitions,
            List<Service> services,
            FileFilter fileFilter,
            string rootPath,
            string platform,
            DefinitionInfo definition)
        {
            // Include the include project's definition.
            fileFilter.ApplyInclude("^" + Regex.Escape("Build/Projects/" + definition.Name + ".definition") + "$");
            fileFilter.ApplyRewrite(
                "^" + Regex.Escape("Build/Projects/" + definition.Name + ".definition") + "$",
                "protobuild/" + platform + "/Build/Projects/" + definition.Name + ".definition");

            // Include everything underneath the include project's path.
            fileFilter.ApplyInclude("^" + Regex.Escape(definition.RelativePath.TrimEnd(new[] { '/', '\\' })) + "/(.+)$");
            fileFilter.ApplyCopy("^" + Regex.Escape(definition.RelativePath.TrimEnd(new[] { '/', '\\' })) + "/(.+)$", "content/$1");
            fileFilter.ApplyRewrite(
                "^" + Regex.Escape(definition.RelativePath.TrimEnd(new[] { '/', '\\' })) + "/(.+)$",
                "protobuild/" + platform + "/" + definition.RelativePath + "$1");
        }
    }
}

