using System;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Protobuild.Services;

namespace Protobuild
{
    internal class AutomaticModulePackager : IAutomaticModulePackager
    {
        private readonly IProjectLoader m_ProjectLoader;

        private readonly IServiceInputGenerator m_ServiceInputGenerator;

        private readonly IProjectOutputPathCalculator m_ProjectOutputPathCalculator;

        public AutomaticModulePackager(
            IProjectLoader projectLoader,
            IServiceInputGenerator serviceInputGenerator,
            IProjectOutputPathCalculator projectOutputPathCalculator)
        {
            this.m_ProjectLoader = projectLoader;
            this.m_ServiceInputGenerator = serviceInputGenerator;
            this.m_ProjectOutputPathCalculator = projectOutputPathCalculator;
        }

        public void Autopackage(
            FileFilter fileFilter,
            Execution execution,
            ModuleInfo module, 
            string rootPath,
            string platform,
            List<string> temporaryFiles)
        {
            var definitions = module.GetDefinitionsRecursively(platform).ToArray();
            var loadedProjects = new List<LoadedDefinitionInfo>();

            foreach (var definition in definitions)
            {
                Console.WriteLine("Loading: " + definition.Name);
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
                    Console.WriteLine("Enabled service: " + service.FullName);
                }
            }

            var packagePaths = module.Packages
                .Select(x => new DirectoryInfo(System.IO.Path.Combine(module.Path, x.Folder)).FullName)
                .ToArray();
            
            foreach (var definition in definitions)
            {
                if (definition.SkipAutopackage)
                {
                    Console.WriteLine("Skipping: " + definition.Name);
                    continue;
                }

                var definitionNormalizedPath = new FileInfo(definition.AbsolutePath).FullName;
                if (packagePaths.Any(definitionNormalizedPath.StartsWith))
                {
                    Console.WriteLine("Skipping: " + definition.Name + " (part of another package)");
                    continue;
                }

                switch (definition.Type)
                {
                    case "External":
                        Console.WriteLine("Packaging: " + definition.Name);
                        this.AutomaticallyPackageExternalProject(definitions, services, fileFilter, rootPath, platform, definition, temporaryFiles);
                        break;
                    case "Include":
                        Console.WriteLine("Packaging: " + definition.Name);
                        this.AutomaticallyPackageIncludeProject(definitions, services, fileFilter, rootPath, platform, definition);
                        break;
                    case "Content":
                        Console.WriteLine("Content project definition skipped: " + definition.Name);
                        break;
                    default:
                        Console.WriteLine("Packaging: " + definition.Name);
                        this.AutomaticallyPackageNormalProject(definitions, services, fileFilter, rootPath, platform, definition, temporaryFiles);
                        break;
                }
            }

            // If there is no Module.xml in the source mappings already, then copy the current module.
            var filterDictionary = fileFilter.ToDictionarySafe(
                k => k.Key, 
                v => v.Value,
                (dict, d) => Console.WriteLine ("WARNING: More than one file maps to " + d.Key));
            if (!filterDictionary.ContainsValue("Build/Module.xml"))
            {
                fileFilter.AddManualMapping(Path.Combine(module.Path, "Build", "Module.xml"), "Build/Module.xml");
            }
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
            fileFilter.AddManualMapping(temp, "Build/Projects/" + definition.Name + ".definition");
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
                                fileFilter.ApplyRewrite(sourcePathRegex, destPathRegex);
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
                            fileFilter.ApplyRewrite(sourcePathRegex, destPathRegex);
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
                            Console.WriteLine(
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
                            Console.WriteLine("WARNING: Unknown tag '" + child.LocalName + "' encountered.");
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
                                var rewriteMatch = fileFilter.ApplyRewrite("^" + pathPrefix + Regex.Escape(assemblyFile) + "$", definition.Name + "/AnyCPU/" + assemblyFile);
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
                            fileFilter.ApplyRewrite("^" + pathPrefix + "(.+)$", definition.Name + "/AnyCPU/$2");

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
                                var rewriteMatch = fileFilter.ApplyRewrite("^" + pathPrefix + Regex.Escape(assemblyFile) + "$", definition.Name + "/" + pathArchReplace + "/" + assemblyFile);
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

                            if (pathArchMatch == "([^/]+)")
                            {
                                fileFilter.ApplyRewrite("^" + pathPrefix + "(.+)$", definition.Name + "/" + pathArchReplace + "/$3");
                            }
                            else
                            {
                                fileFilter.ApplyRewrite("^" + pathPrefix + "(.+)$", definition.Name + "/" + pathArchReplace + "/$2");
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

                    Console.WriteLine("WARNING: There is more than one project with the name " +
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
                            Console.WriteLine(
                                "WARNING: Copy-on-build file '" + link + "' in library project which " +
                                "does not output to root of project detected.  This is not supported.");
                        }
                        else
                        {
                            if (fileInfo.Name != link)
                            {
                                Console.WriteLine(
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
                                fileFilter.ApplyRewrite(sourcePathRegex, destPathRegex);
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
            fileFilter.AddManualMapping(temp, "Build/Projects/" + definition.Name + ".definition");
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

            // Include everything underneath the include project's path.
            fileFilter.ApplyInclude("^" + Regex.Escape(definition.RelativePath) + ".+$");
        }
    }
}

