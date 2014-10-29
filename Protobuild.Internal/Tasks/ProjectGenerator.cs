using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Protobuild.Tasks
{
    using System.Runtime.InteropServices.ComTypes;
    using Protobuild.Services;

    public class ProjectGenerator
    {
        private XslCompiledTransform m_ProjectTransform = null;
        private XslCompiledTransform m_GenerateSolutionTransform = null;
        private XslCompiledTransform m_SelectSolutionTransform = null;
        private Action<string> m_Log;

        public ProjectGenerator(
            string rootPath,
            string platform,
            Action<string> log)
        {
            this.Documents = new List<XmlDocument>();
            this.RootPath = rootPath;
            this.Platform = platform;
            this.m_Log = log;
        }

        public List<XmlDocument> Documents { get; private set; }

        public string RootPath { get; private set; }

        public string Platform { get; private set; }

        public void Load(string path, string rootPath = null, string modulePath = null)
        {
            var doc = new XmlDocument();
            doc.Load(path);

            // If this is a ContentProject, we actually need to generate the
            // full project node from the files that are in the Source folder.
            XmlDocument newDoc = null;
            if (doc.DocumentElement.Name == "ContentProject")
                newDoc = GenerateContentProject(doc, modulePath);
            else
                newDoc = doc;
            if (rootPath != null && modulePath != null)
            {
                var additionalPath = modulePath.Substring(rootPath.Length).Replace('/', '\\');
                if (newDoc.DocumentElement != null &&
                    newDoc.DocumentElement.Attributes["Path"] != null &&
                    additionalPath != null)
                {
                    newDoc.DocumentElement.Attributes["Path"].Value =
                        (additionalPath.Trim('\\') + '\\' +
                        newDoc.DocumentElement.Attributes["Path"].Value).Trim('\\');
                }
                if (newDoc.DocumentElement.Name == "ExternalProject")
                {
                    // Need to find all descendant Binary and Project tags
                    // and update their paths as well.
                    var xDoc = newDoc.ToXDocument();
                    var projectsToUpdate = xDoc.Descendants().Where(x => x.Name == "Project");
                    var binariesToUpdate = xDoc.Descendants().Where(x => x.Name == "Binary");
                    foreach (var projectToUpdate in projectsToUpdate
                        .Where(x => x.Attribute("Path") != null))
                    {
                        projectToUpdate.Attribute("Path").Value =
                            (additionalPath.Trim('\\') + '\\' +
                            projectToUpdate.Attribute("Path").Value).Replace('/', '\\').Trim('\\');
                    }
                    foreach (var binaryToUpdate in binariesToUpdate
                        .Where(x => x.Attribute("Path") != null))
                    {
                        binaryToUpdate.Attribute("Path").Value =
                            (additionalPath.Trim('\\') + '\\' +
                            binaryToUpdate.Attribute("Path").Value).Replace('/', '\\').Trim('\\');
                    }
                    newDoc = xDoc.ToXmlDocument();
                }
            }
            this.Documents.Add(newDoc);

            // If the Guid property doesn't exist, we do one of two things:
            //  * Check for the existance of a Guid under the ProjectGuids tag
            //  * Autogenerate a Guid for the project
            if (doc.DocumentElement.Attributes["Guid"] == null)
            {
                var autogenerate = true;
                var projectGuids = doc.DocumentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name == "ProjectGuids");
                if (projectGuids != null)
                {
                    var platform = projectGuids.ChildNodes.OfType<XmlElement>().FirstOrDefault(x =>
                        x.Name == "Platform" && x.HasAttribute("Name") && x.GetAttribute("Name") == this.Platform);
                    if (platform != null)
                    {
                        autogenerate = false;
                        doc.DocumentElement.SetAttribute("Guid", platform.InnerText.Trim().ToUpper());
                    }
                }

                if (autogenerate)
                {
                    var name = doc.DocumentElement.GetAttribute("Name") + "." + this.Platform;
                    var guidBytes = new byte[16];
                    for (var i = 0; i < guidBytes.Length; i++)
                        guidBytes[i] = (byte)0;
                    var nameBytes = Encoding.ASCII.GetBytes(name);
                    unchecked
                    {
                        for (var i = 0; i < nameBytes.Length; i++)
                            guidBytes[i % 16] += nameBytes[i];
                        for (var i = nameBytes.Length; i < 16; i++)
                            guidBytes[i] += nameBytes[i % nameBytes.Length];
                    }
                    var guid = new Guid(guidBytes);
                    doc.DocumentElement.SetAttribute("Guid", guid.ToString().ToUpper());
                }
            }
        }

        /// <summary>
        /// Generates a project at the target path.
        /// </summary>
        /// <param name="project">The path to the project file.</param>
        /// <param name="services"></param>
        /// <param name="packagesFilePath">
        /// Either the full path to the packages.config for the
        /// generated project if it exists, or an empty string.
        /// </param>
        /// <param name="onActualGeneration"></param>
        public void Generate(string project, List<Service> services, out string packagesFilePath, Action onActualGeneration)
        {
            packagesFilePath = "";

            if (this.m_ProjectTransform == null)
            {
                this.m_ProjectTransform = XSLTLoader.LoadGenerateProjectXSLT(this.RootPath);
            }

            // Work out what document this is.
            var projectDoc = this.Documents.First(
                x => x.DocumentElement.Attributes["Name"].Value == project);

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
                    if (string.Compare(this.Platform, platform, StringComparison.InvariantCultureIgnoreCase) == 0)
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
            if (this.IsExcludedServiceAwareProject(projectDoc.DocumentElement.Attributes["Name"].Value, projectDoc, services))
            {
                return;
            }

            // Inform the user we're generating this project.
            onActualGeneration();

            // Work out what path to save at.
            var path = Path.Combine(
                this.RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar),
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                this.Platform + ".csproj");

            // Make sure that the directory exists where the file will be stored.
            var targetFile = new FileInfo(path);
            if (!targetFile.Directory.Exists)
                targetFile.Directory.Create();

            path = targetFile.FullName;

            // Handle NuGet packages.config early so that it'll be in place
            // when the generator automatically determined dependencies.
            this.HandleNuGetConfig(projectDoc);

            // Work out what path the NuGet packages.config might be at.
            var packagesFile = new FileInfo(
                Path.Combine(
                    this.RootPath,
                    projectDoc.DocumentElement.Attributes["Path"].Value
                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar),
                    "packages.config"));

            // Generate the input document.
            var input = this.CreateInputFor(
                project,
                this.Platform,
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
                this.m_ProjectTransform.Transform(input, writer);
            }

            // Also remove any left over .sln or .userprefs files.
            var slnPath = Path.Combine(
                this.RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value,
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                this.Platform + ".sln");
            var userprefsPath = Path.Combine(
                this.RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value,
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                this.Platform + ".userprefs");
            if (File.Exists(slnPath))
                File.Delete(slnPath);
            if (File.Exists(userprefsPath))
                File.Delete(userprefsPath);

            // Only return the package file path if it exists.
            if (packagesFile.Exists)
                packagesFilePath = packagesFile.FullName;
        }

        private bool IsExcludedServiceAwareProject(string name, XmlDocument projectDoc, List<Service> services)
        {
            return projectDoc.DocumentElement.ChildNodes.OfType<XmlElement>().Any(x => x.Name == "Services")
                   && services.All(x => x.ProjectName != name);
        }

        private void HandleNuGetConfig(XmlDocument projectDoc)
        {
            var srcPath = Path.Combine(
                this.RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar),
                "packages." + this.Platform + ".config");
            var destPath = Path.Combine(
                this.RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar),
                "packages.config");

            if (File.Exists(srcPath))
            {
                File.Copy(srcPath, destPath, true);
            }
        }

        public void GenerateSolution(ModuleInfo moduleInfo, string solutionPath, List<Service> services, IEnumerable<string> repositoryPaths)
        {
            if (this.m_GenerateSolutionTransform == null)
            {
                this.m_GenerateSolutionTransform = XSLTLoader.LoadNormalXSLT(this.RootPath, "GenerateSolution");
            }

            if (this.m_SelectSolutionTransform == null)
            {
                this.m_SelectSolutionTransform = XSLTLoader.LoadNormalXSLT(this.RootPath, "SelectSolution");
            }

            var input = this.CreateInputForSelectSolution(this.Platform, services);
            using (var memory = new MemoryStream())
            {
                this.m_SelectSolutionTransform.Transform(input, null, memory);

                memory.Seek(0, SeekOrigin.Begin);

                var document = new XmlDocument();
                document.Load(memory);

                var defaultProject = (XmlElement)null;
                var existingGuids = new List<string>();
                foreach (var element in document.DocumentElement.SelectNodes("/Projects/Project").OfType<XmlElement>().ToList())
                {
                    var f = element.SelectNodes("Guid").OfType<XmlElement>().FirstOrDefault();

                    if (f != null)
                    {
                        if (existingGuids.Contains(f.InnerText.Trim()))
                        {
                            element.ParentNode.RemoveChild(element);
                            continue;
                        }
                        else
                        {
                            existingGuids.Add(f.InnerText.Trim());
                        }
                    }

                    var n = element.SelectNodes("RawName").OfType<XmlElement>().FirstOrDefault();

                    if (n != null)
                    {
                        if (n.InnerText.Trim() == moduleInfo.DefaultStartupProject)
                        {
                            defaultProject = element;
                        }
                    }
                }

                if (defaultProject != null)
                {
                    // Move the default project to the first element of it's parent.  The first project
                    // in the solution is the default startup project.
                    var parent = defaultProject.ParentNode;
                    parent.RemoveChild(defaultProject);
                    parent.InsertBefore(defaultProject, parent.FirstChild);
                }

                var documentInput = this.CreateInputForGenerateSolution(
                    this.Platform,
                    document.DocumentElement.SelectNodes("/Projects/Project").OfType<XmlElement>());

                using (var writer = new StreamWriter(solutionPath))
                {
                    this.m_GenerateSolutionTransform.Transform(documentInput, null, writer);
                }
            }

            if (repositoryPaths != null && repositoryPaths.Any())
            {
                GenerateRepositoriesConfig(solutionPath, repositoryPaths);
            }
        }

        private XmlDocument CreateInputFor(string project, string platform, string packagesPath, IEnumerable<XmlElement> properties, List<Service> services)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var input = doc.CreateElement("Input");
            doc.AppendChild(input);

            input.AppendChild(this.CreateServicesInputFor(doc, project, services));

            var generation = doc.CreateElement("Generation");
            var projectName = doc.CreateElement("ProjectName");
            projectName.AppendChild(doc.CreateTextNode(project));
            var platformName = doc.CreateElement("Platform");
            platformName.AppendChild(doc.CreateTextNode(platform));
            var hostPlatformName = doc.CreateElement("HostPlatform");
            hostPlatformName.AppendChild(doc.CreateTextNode(Actions.DetectPlatform()));

            if (string.Compare(platform, "Web", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // Add JSIL properties
                string jsilDirectory, jsilCompilerFile;
                var jsilProvider = new JSILProvider();
                if (!jsilProvider.GetJSIL(out jsilDirectory, out jsilCompilerFile))
                {
                    throw new InvalidOperationException("JSIL not found, but previous check passed.");
                }

                var jsilDirectoryNode = doc.CreateElement("JSILDirectory");
                jsilDirectoryNode.AppendChild(doc.CreateTextNode(jsilDirectory));
                generation.AppendChild(jsilDirectoryNode);
                var jsilCompilerPathNode = doc.CreateElement("JSILCompilerFile");
                jsilCompilerPathNode.AppendChild(doc.CreateTextNode(jsilCompilerFile));
                generation.AppendChild(jsilCompilerPathNode);

                var jsilLibrariesNode = doc.CreateElement("JSILLibraries");

                foreach (var entry in jsilProvider.GetJSILLibraries())
                {
                    var entryNode = doc.CreateElement("Library");
                    var pathAttribute = doc.CreateAttribute("Path");
                    pathAttribute.Value = entry.Key;
                    var nameAttribute = doc.CreateAttribute("Name");
                    nameAttribute.Value = entry.Value;
                    entryNode.Attributes.Append(pathAttribute);
                    entryNode.Attributes.Append(nameAttribute);
                    jsilLibrariesNode.AppendChild(entryNode);
                }

                generation.AppendChild(jsilLibrariesNode);

                // Automatically extract the JSIL template if not already present.
                var currentProject =
                    this.Documents.Select(x => x.DocumentElement)
                        .Where(x => x.Attributes != null)
                        .Where(x => x.Attributes["Name"] != null)
                        .FirstOrDefault(x => x.Attributes["Name"].Value == project);
                if (currentProject != null)
                {
                    string type = null;
                    string path = null;
                    if (currentProject.Attributes != null && currentProject.Attributes["Type"] != null)
                    {
                        type = currentProject.Attributes["Type"].Value;
                    }
                    if (currentProject.Attributes != null && currentProject.Attributes["Path"] != null)
                    {
                        path = currentProject.Attributes["Path"].Value;
                    }

                    if (string.Compare(type, "App", StringComparison.InvariantCultureIgnoreCase) == 0
                        || string.Compare(type, "Console", StringComparison.InvariantCultureIgnoreCase) == 0
                        || string.Compare(type, "GUI", StringComparison.InvariantCultureIgnoreCase) == 0
                        || string.Compare(type, "GTK", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        if (path != null)
                        {
                            var srcDir = Path.Combine(this.RootPath, path);
                            if (Directory.Exists(srcDir))
                            {
                                if (!File.Exists(Path.Combine(srcDir, "index.htm")))
                                {
                                    Console.WriteLine("Extracting JSIL HTML template...");
                                    ResourceExtractor.ExtractJSILTemplate(project, Path.Combine(srcDir, "index.htm"));
                                }
                            }
                        }
                    }
                }
            }

            var rootName = doc.CreateElement("RootPath");
            rootName.AppendChild(doc.CreateTextNode(
                new DirectoryInfo(this.RootPath).FullName));
            var useCSCJVM = doc.CreateElement("UseCSCJVM");
            useCSCJVM.AppendChild(doc.CreateTextNode(
                this.IsUsingCSCJVM(platform) ? "True" : "False"));
            generation.AppendChild(projectName);
            generation.AppendChild(platformName);
            generation.AppendChild(hostPlatformName);
            generation.AppendChild(rootName);
            generation.AppendChild(useCSCJVM);
            input.AppendChild(generation);

            var propertiesNode = doc.CreateElement("Properties");
            foreach (var property in properties)
            {
                if (property.Name.ToLower() == "property")
                {
                    var nodeName = doc.CreateElement(property.GetAttribute("Name"));
                    nodeName.AppendChild(doc.CreateTextNode(
                        property.GetAttribute("Value")));
                    propertiesNode.AppendChild(nodeName);
                }
                else
                    propertiesNode.AppendChild(
                        doc.ImportNode(property, true));
            }
            input.AppendChild(propertiesNode);

            var nuget = doc.CreateElement("NuGet");
            input.AppendChild(nuget);

            var projects = doc.CreateElement("Projects");
            input.AppendChild(projects);
            foreach (var projectDoc in this.Documents)
            {
                projects.AppendChild(
                    this.UpdateProjectsWithServiceReferences(doc.ImportNode(projectDoc.DocumentElement, true), services));
            }

            // Also check if there are NuGet packages.config file for
            // this project and if there is, include all of the relevant
            // NuGet package information for referencing the correct DLLs.
            if (File.Exists(packagesPath))
            {
                this.DetectNuGetPackages(
                    packagesPath,
                    doc,
                    nuget);
            }

            return doc;
        }

        private XmlNode UpdateProjectsWithServiceReferences(XmlNode importNode, List<Service> services)
        {
            var element = importNode as XmlElement;

            if (element == null || element.Name != "Project")
            {
                return importNode;
            }

            if (importNode.OwnerDocument == null)
            {
                return importNode;
            }

            var references = element.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name == "References");

            if (references == null)
            {
                references = importNode.OwnerDocument.CreateElement("References");
                importNode.OwnerDocument.DocumentElement.AppendChild(references);
            }

            var lookup = services.ToDictionary(k => k.FullName, v => v);

            var projectName = element.GetAttribute("Name");

            foreach (var service in services.Where(x => x.ProjectName == projectName))
            {
                foreach (var req in service.Requires.Select(x => lookup[x]))
                {
                    if (projectName == req.ProjectName)
                    {
                        continue;
                    }

                    if (!req.InfersReference)
                    {
                        continue;
                    }

                    if (
                        !references.ChildNodes.OfType<XmlElement>()
                             .Any(x => x.Name == "Reference" && x.GetAttribute("Include") == req.ProjectName))
                    {
                        var referenceElement = importNode.OwnerDocument.CreateElement("Reference");
                        referenceElement.SetAttribute("Include", req.ProjectName);
                        references.AppendChild(referenceElement);
                    }
                }
            }

            foreach (var service in services.Where(x => x.ProjectName == projectName))
            {
                foreach (var addRef in service.AddReferences)
                {
                    if (
                        !references.ChildNodes.OfType<XmlElement>()
                             .Any(x => x.Name == "Reference" && x.GetAttribute("Include") == addRef))
                    {
                        var referenceElement = importNode.OwnerDocument.CreateElement("Reference");
                        referenceElement.SetAttribute("Include", addRef);
                        references.AppendChild(referenceElement);
                    }
                }
            }

            return importNode;
        }

        private XmlNode CreateServicesInputFor(XmlDocument doc, string projectName, IEnumerable<Service> services)
        {
            var servicesElements = doc.CreateElement("Services");
            string activeServiceNames = null;

            foreach (var service in services)
            {
                var serviceElement = doc.CreateElement("Service");
                serviceElement.SetAttribute("Name", service.FullName);
                serviceElement.SetAttribute("Project", service.ProjectName);
                this.AddList(doc, serviceElement, service.AddDefines, "AddDefines", "AddDefine");
                this.AddList(doc, serviceElement, service.RemoveDefines, "RemoveDefines", "RemoveDefine");
                this.AddList(doc, serviceElement, service.AddReferences, "AddReferences", "AddReference");
                servicesElements.AppendChild(serviceElement);

                if (activeServiceNames == null)
                {
                    activeServiceNames = service.FullName;
                }
                else
                {
                    activeServiceNames += "," + service.FullName;
                }

                if (projectName != null)
                {
                    if (!string.IsNullOrEmpty(service.ServiceName))
                    {
                        if (service.ProjectName == projectName)
                        {
                            // Include relative service name in list.
                            activeServiceNames += "," + service.ServiceName;
                        }
                    }
                }
            }

            var activeServicesNamesElement = doc.CreateElement("ActiveServicesNames");
            activeServicesNamesElement.InnerText = activeServiceNames ?? string.Empty;
            servicesElements.AppendChild(activeServicesNamesElement);

            return servicesElements;
        }

        private void AddList(XmlDocument doc, XmlElement serviceElement, IEnumerable<string> entries, string containerName, string entryName)
        {
            var element = doc.CreateElement(containerName);
            serviceElement.AppendChild(element);

            foreach (var entry in entries)
            {
                var entryElement = doc.CreateElement(entryName);
                entryElement.InnerText = entry;
                element.AppendChild(entryElement);
            }
        }

        private void DetectNuGetPackages(
            string packagesPath,
            XmlDocument document,
            XmlNode nuget)
        {
            // Read the packages document and generate Project nodes for
            // each package that we want.
            var packagesDoc = new XmlDocument();
            packagesDoc.Load(packagesPath);
            var packages = packagesDoc.DocumentElement
                .ChildNodes
                    .OfType<XmlElement>()
                    .Where(x => x.Name == "package")
                    .Select(x => x);
            foreach (var package in packages)
            {
                var id = package.Attributes["id"].Value;
                var version = package.Attributes["version"].Value;
                var targetFramework = (package.Attributes["targetFramework"] != null ? package.Attributes["targetFramework"].Value : null) ?? "";

                var packagePath = Path.Combine(
                    this.RootPath,
                    "packages",
                    id + "." + version,
                    id + "." + version + ".nuspec");

                // Use the nuspec file if it exists.
                List<string> references = new List<string>();
                if (File.Exists(packagePath))
                {
                    var packageDoc = new XmlDocument();
                    packageDoc.Load(packagePath);

                    // If the references are explicitly provided in the nuspec, use
                    // those as to what files should be referenced by the projects.
                    if (
                        packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
                            .Count(x => x.Name == "references") > 0)
                    {
                        references =
                            packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
                                .First(x => x.Name == "references")
                                .ChildNodes.OfType<XmlElement>()
                                .Where(x => x.Name == "reference")
                                .Select(x => x.Attributes["file"].Value)
                                .ToList();
                    }
                }

                // Determine the priority of the frameworks that we want to target
                // out of the available versions.
                string[] clrNames = new[]
                {
                    targetFramework,
                    "net40-client",
                    "Net40-client",
                    "net40",
                    "Net40",
                    "net35",
                    "Net35",
                    "net20",
                    "Net20",
                    "20",
                    "11",
                    ""
                };

                // Determine the base path for all references; that is, the lib/ folder.
                var referenceBasePath = Path.Combine(
                    "packages",
                    id + "." + version,
                    "lib");

                // If we don't have a lib/ folder, then we aren't able to reference anything
                // anyway (this might be a tools only package like xunit.runners).
                if (!Directory.Exists(
                    Path.Combine(
                    this.RootPath,
                    referenceBasePath)))
                    continue;

                // If no references are in nuspec, reference all of the libraries that
                // are on disk.
                if (references.Count == 0)
                {
                    // Search through all of the target frameworks until we find one that
                    // has at least one file in it.
                    foreach (var clrName in clrNames)
                    {
                        var foundClr = false;

                        // If this target framework doesn't exist for this library, skip it.
                        if (!Directory.Exists(
                            Path.Combine(
                            this.RootPath,
                            referenceBasePath,
                            clrName)))
                            continue;

                        // Otherwise enumerate through all of the libraries in this folder.
                        foreach (var dll in Directory.EnumerateFiles(
                            Path.Combine(
                            this.RootPath,
                            referenceBasePath, clrName),
                            "*.dll"))
                        {
                            // Determine the relative path to the library.
                            var packageDll = Path.Combine(
                                referenceBasePath,
                                clrName,
                                Path.GetFileName(dll));

                            // Confirm again that the file actually exists on disk when
                            // combined with the root path.
                            if (File.Exists(
                                Path.Combine(
                                this.RootPath,
                                packageDll)))
                            {
                                // Create the library reference.
                                var packageReference =
                                    document.CreateElement("Package");
                                packageReference.SetAttribute(
                                    "Name",
                                    Path.GetFileNameWithoutExtension(dll));
                                packageReference.AppendChild(
                                    document.CreateTextNode(packageDll
                                    .Replace('/', '\\')));
                                nuget.AppendChild(packageReference);

                                // Mark this target framework as having provided at least
                                // one reference.
                                foundClr = true;
                            }
                        }

                        // Break if we have found at least one reference.
                        if (foundClr)
                            break;
                    }
                }

                // For all of the references that were found in the original nuspec file,
                // add those references.
                foreach (var reference in references)
                {
                    // Search through all of the target frameworks until we find the one
                    // that has the reference in it.
                    foreach (var clrName in clrNames)
                    {
                        // If this target framework doesn't exist for this library, skip it.
                        var packageDll = Path.Combine(
                            referenceBasePath,
                            clrName,
                            reference);

                        if (File.Exists(
                            Path.Combine(
                            this.RootPath,
                            packageDll)))
                        {
                            // Create the library reference.
                            var packageReference =
                                document.CreateElement("Package");
                            packageReference.SetAttribute(
                                "Name",
                                id);
                            packageReference.AppendChild(
                                document.CreateTextNode(packageDll
                                .Replace('/', '\\')));
                            nuget.AppendChild(packageReference);
                            break;
                        }
                    }
                }


            }
        }

        private XmlDocument GenerateContentProject(XmlDocument source, string rootPath)
        {
            var allFiles = new List<KeyValuePair<string, IEnumerable<string>>>();
            string sourceFile = null;
            string sourceFileFolder = null;
            foreach (var element in source
                .DocumentElement
                .ChildNodes
                .OfType<XmlElement>()
                .Where(x => x.Name == "Source"))
            {
                var sourceFolder = element.GetAttribute("Include");
                //Pattern matching to enable platform specific content
                if (sourceFolder.Contains("$(Platform)"))
                {
                    sourceFolder = sourceFolder.Replace("$(Platform)", this.Platform);
                }
                var matchFiles = element.GetAttribute("Match");
                var originalSourceFolder = sourceFolder;
                if (element.HasAttribute("Primary") && element.GetAttribute("Primary").ToLower() == "true")
                {
                    sourceFileFolder = Path.Combine(rootPath, sourceFolder);
                    sourceFile = Path.Combine(rootPath, sourceFolder, ".source");
                    using (var writer = new StreamWriter(sourceFile))
                    {
                        var dir = new DirectoryInfo(sourceFileFolder);
                        writer.Write(dir.FullName);
                    }
                }
                sourceFolder = Path.Combine(rootPath, sourceFolder);
                var files = this.GetListOfFilesInDirectory(sourceFolder, matchFiles);
                allFiles.Add(
                    new KeyValuePair<string, IEnumerable<string>>(
                        originalSourceFolder,
                        files));
                this.m_Log(
                  "Scanning: " +
                  originalSourceFolder +
                  " (" + files.Count + " total files)"
                  );
            }

            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var projectNode = doc.CreateElement("ContentProject");
            doc.AppendChild(projectNode);
            projectNode.SetAttribute(
                "Name",
                source.DocumentElement.GetAttribute("Name"));

            foreach (var kv in allFiles)
            {
                var originalSourceFolder = kv.Key;
                foreach (var file in kv.Value)
                {
                    var fileNode = doc.CreateElement("Compiled");
                    var fullPathNode = doc.CreateElement("FullPath");
                    var relativePathNode = doc.CreateElement("RelativePath");
                    fullPathNode.AppendChild(doc.CreateTextNode(file));
                    var index = file.Replace("\\", "/")
                        .LastIndexOf(originalSourceFolder.Replace("\\", "/"));
                    var relativePath = "\\" + file
                        .Substring(index + originalSourceFolder.Length)
                        .Replace("/", "\\")
                        .Trim('\\');
                    relativePathNode.AppendChild(doc.CreateTextNode(relativePath));
                    fileNode.AppendChild(fullPathNode);
                    fileNode.AppendChild(relativePathNode);
                    projectNode.AppendChild(fileNode);
                }
            }

            if (sourceFile != null)
            {
                var fileNode = doc.CreateElement("Compiled");
                var fullPathNode = doc.CreateElement("FullPath");
                var relativePathNode = doc.CreateElement("RelativePath");
                fullPathNode.AppendChild(doc.CreateTextNode(sourceFile));
                var index = sourceFile.Replace("\\", "/")
                    .LastIndexOf(sourceFileFolder.Replace("\\", "/"));
                var relativePath = "\\" + sourceFile
                    .Substring(index + sourceFileFolder.Length)
                    .Replace("/", "\\")
                    .Trim('\\');
                relativePathNode.AppendChild(doc.CreateTextNode(relativePath));
                fileNode.AppendChild(fullPathNode);
                fileNode.AppendChild(relativePathNode);
                projectNode.AppendChild(fileNode);
            }

            return doc;
        }

        private static void GenerateRepositoriesConfig(
            string solutionPath,
            IEnumerable<string> repositoryPaths)
        {
            FileInfo repositoriesFile = new FileInfo(
                Path.Combine(
                    new FileInfo(solutionPath).Directory.FullName, 
                    "packages", 
                    "repositories.config"));
            Uri repositoriesUri = new Uri(repositoriesFile.FullName);

            // Always refresh this file.
            if (repositoriesFile.Exists)
                repositoriesFile.Delete();
            else if (!repositoriesFile.Directory.Exists)
                repositoriesFile.Directory.Create();

            // Write out the xml to disk.
            using (var writer = new StreamWriter(repositoriesFile.FullName))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                writer.WriteLine("<repositories>");
                foreach (string path in repositoryPaths.OrderBy(p => p))
                {
                    writer.Write("  <repository path=\"");

                    // Write a relational path to the config from the repository.config.
                    Uri current = new Uri(path);
                    writer.Write(
                        Uri.UnescapeDataString(
                            repositoriesUri.MakeRelativeUri(current)
                                .ToString()
                               .Replace('/', Path.DirectorySeparatorChar)));
                    writer.WriteLine("\" />");
                }
                writer.WriteLine("</repositories>");
            }
        }

        private List<string> GetListOfFilesInDirectory(string folder, string match)
        {
            var result = new List<string>();
            var directoryInfo = new DirectoryInfo(folder);
            foreach (var directory in directoryInfo.GetDirectories())
            {
                result.AddRange(
                    this.GetListOfFilesInDirectory(directory.FullName, match));
            }
            foreach (var file in directoryInfo.GetFiles(match))
            {
                result.Add(file.FullName);
            }
            return result;
        }

        private bool IsUsingCSCJVM(string platform)
        {
            if (platform.ToLower() == "java")
                return true;
            return false;
        }

        private XmlDocument CreateInputForSelectSolution(string platform, List<Service> services)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var input = doc.CreateElement("Input");
            doc.AppendChild(input);

            input.AppendChild(this.CreateServicesInputFor(doc, null, services));

            var generation = doc.CreateElement("Generation");
            var platformName = doc.CreateElement("Platform");
            platformName.AppendChild(doc.CreateTextNode(platform));
            var useCSCJVM = doc.CreateElement("UseCSCJVM");
            useCSCJVM.AppendChild(doc.CreateTextNode(
                this.IsUsingCSCJVM(platform) ? "True" : "False"));
            generation.AppendChild(useCSCJVM);
            generation.AppendChild(platformName);
            input.AppendChild(generation);

            var projects = doc.CreateElement("Projects");
            input.AppendChild(projects);
            foreach (var projectDoc in this.Documents)
            {
                if (this.IsExcludedServiceAwareProject(
                    projectDoc.DocumentElement.GetAttribute("Name"),
                    projectDoc,
                    services))
                {
                    continue;
                }

                projects.AppendChild(doc.ImportNode(
                    projectDoc.DocumentElement,
                    true));
            }
            return doc;
        }

        private XmlDocument CreateInputForGenerateSolution(string platform, IEnumerable<XmlElement> projectElements)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var input = doc.CreateElement("Input");
            doc.AppendChild(input);

            var generation = doc.CreateElement("Generation");
            var platformName = doc.CreateElement("Platform");
            platformName.AppendChild(doc.CreateTextNode(platform));
            var useCSCJVM = doc.CreateElement("UseCSCJVM");
            useCSCJVM.AppendChild(doc.CreateTextNode(
                this.IsUsingCSCJVM(platform) ? "True" : "False"));
            generation.AppendChild(useCSCJVM);
            generation.AppendChild(platformName);
            input.AppendChild(generation);

            var projects = doc.CreateElement("Projects");
            input.AppendChild(projects);

            foreach (var projectElem in projectElements)
            {
                projects.AppendChild(doc.ImportNode(projectElem, true));
            }

            return doc;
        }
    }
}

