using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Xsl;
using Microsoft.Build.Utilities;
using System.Text;

namespace Protobuild.Tasks
{
    public class ProjectGenerator
    {
        private string m_RootPath;
        private string m_Platform;
        private List<XmlDocument> m_ProjectDocuments = new List<XmlDocument>();
        private XslCompiledTransform m_NuspecTransform = null;
        private XslCompiledTransform m_ProjectTransform = null;
        private XslCompiledTransform m_SolutionTransform = null;
        private Action<string> m_Log;

        public ProjectGenerator(
            string rootPath,
            string platform,
            Action<string> log)
        {
            this.m_RootPath = rootPath;
            this.m_Platform = platform;
            this.m_Log = log;
        }

        public void Load(string path, string rootPath = null, string modulePath = null)
        {
            var doc = new XmlDocument();
            doc.Load(path);

            // If this is a ContentProject, we actually need to generate the
            // full project node from the files that are in the Source folder.
            XmlDocument newDoc = null;
            if (doc.DocumentElement.Name == "ContentProject")
                newDoc = GenerateContentProject(doc);
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
            this.m_ProjectDocuments.Add(newDoc);

            // Also add a Guid attribute if one doesn't exist.  This makes it
            // easier to define projects.
            if (doc.DocumentElement.Attributes["Guid"] == null)
            {
                var name = doc.DocumentElement.GetAttribute("Name");
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

        public void Generate(string project)
        {
            if (this.m_ProjectTransform == null)
            {
                var resolver = new EmbeddedResourceResolver();
                this.m_ProjectTransform = new XslCompiledTransform();
                using (var reader = XmlReader.Create(ResourceExtractor.GetGenerateProjectXSLT(this.m_RootPath)))
                {
                    this.m_ProjectTransform.Load(
                        reader,
                        XsltSettings.TrustedXslt,
                        resolver
                    );
                }
            }
            if (this.m_NuspecTransform == null)
            {
                var resolver = new EmbeddedResourceResolver();
                this.m_NuspecTransform = new XslCompiledTransform();
                Stream generateNuspecStream;
                var generateNuspecXSLT = Path.Combine(this.m_RootPath, "Build", "GenerateNuspec.xslt");
                if (File.Exists(generateNuspecXSLT))
                    generateNuspecStream = File.Open(generateNuspecXSLT, FileMode.Open);
                else
                    generateNuspecStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                        "Protobuild.BuildResources.GenerateNuspec.xslt");
                using (generateNuspecStream)
                {
                    using (var reader = XmlReader.Create(generateNuspecStream))
                    {
                        this.m_NuspecTransform.Load(
                            reader,
                            XsltSettings.TrustedXslt,
                            resolver
                        );
                    }
                }
            }

            // Work out what document this is.
            var projectDoc = this.m_ProjectDocuments.First(
                x => x.DocumentElement.Attributes["Name"].Value == project);

            // Check to see if we have a Project node; if not
            // then this is an external or other type of project
            // that we don't process.
            if (projectDoc == null ||
                projectDoc.DocumentElement.Name != "Project")
                return;

            // Work out what path to save at.
            var path = Path.Combine(
                this.m_RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar),
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                this.m_Platform + ".csproj");
            path = new FileInfo(path).FullName;

            // Work out what path the NuGet packages.config might be at.
            var packagesPath = Path.Combine(
                this.m_RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value,
                "packages.config");

            // Generate the input document.
            var input = this.CreateInputFor(
                project,
                this.m_Platform,
                packagesPath,
                projectDoc.DocumentElement.ChildNodes
                    .Cast<XmlElement>()
                    .Where(x => x.Name.ToLower() == "properties")
                    .SelectMany(x => x.ChildNodes
                        .Cast<XmlElement>()));

            // Transform the input document using the XSLT transform.
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var writer = XmlWriter.Create(path, settings))
            {
                this.m_ProjectTransform.Transform(input, writer);
            }

            // Generate NuSpec target.
            var nuspecPath = path.Substring(0, path.Length - ".csproj".Length) + ".nuspec";
            using (var memory = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memory, settings))
                {
                    this.m_NuspecTransform.Transform(input, writer);
                    writer.Flush();
                }
                if (memory.Position > 0)
                {
                    memory.Seek(0, SeekOrigin.Begin);
                    using (var writer = new FileStream(nuspecPath, FileMode.Create))
                    {
                        memory.CopyTo(writer);
                        writer.Flush();
                    }
                }
            }

            // Also remove any left over .sln or .userprefs files.
            var slnPath = Path.Combine(
                this.m_RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value,
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                this.m_Platform + ".sln");
            var userprefsPath = Path.Combine(
                this.m_RootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value,
                projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                this.m_Platform + ".userprefs");
            if (File.Exists(slnPath))
                File.Delete(slnPath);
            if (File.Exists(userprefsPath))
                File.Delete(userprefsPath);
        }

        public void GenerateSolution(string solutionPath)
        {
            if (this.m_SolutionTransform == null)
            {
                var resolver = new EmbeddedResourceResolver();
                this.m_SolutionTransform = new XslCompiledTransform();
                Stream generateSolutionStream;
                var generateSolutionXSLT = Path.Combine(this.m_RootPath, "Build", "GenerateSolution.xslt");
                if (File.Exists(generateSolutionXSLT))
                    generateSolutionStream = File.Open(generateSolutionXSLT, FileMode.Open);
                else
                    generateSolutionStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                        "Protobuild.BuildResources.GenerateSolution.xslt");
                using (generateSolutionStream)
                {
                    using (var reader = XmlReader.Create(generateSolutionStream))
                    {
                        this.m_SolutionTransform.Load(
                            reader,
                            XsltSettings.TrustedXslt,
                            resolver
                        );
                    }
                }
            }

            var input = this.CreateInputFor(this.m_Platform);
            using (var writer = new StreamWriter(solutionPath))
            {
                this.m_SolutionTransform.Transform(input, null, writer);
            }
        }

        private XmlDocument CreateInputFor(
            string project,
            string platform,
            string packagesPath,
            IEnumerable<XmlElement> properties)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var input = doc.CreateElement("Input");
            doc.AppendChild(input);

            var generation = doc.CreateElement("Generation");
            var projectName = doc.CreateElement("ProjectName");
            projectName.AppendChild(doc.CreateTextNode(project));
            var platformName = doc.CreateElement("Platform");
            platformName.AppendChild(doc.CreateTextNode(platform));
            var rootName = doc.CreateElement("RootPath");
            rootName.AppendChild(doc.CreateTextNode(
                new DirectoryInfo(this.m_RootPath).FullName));
            var useCSCJVM = doc.CreateElement("UseCSCJVM");
            useCSCJVM.AppendChild(doc.CreateTextNode(
                this.IsUsingCSCJVM(platform) ? "True" : "False"));
            generation.AppendChild(projectName);
            generation.AppendChild(platformName);
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
            foreach (var projectDoc in this.m_ProjectDocuments)
            {
                projects.AppendChild(doc.ImportNode(
                    projectDoc.DocumentElement,
                    true));
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
                    .Cast<XmlElement>()
                    .Where(x => x.Name == "package")
                    .Select(x => x);
            foreach (var package in packages)
            {
                var id = package.Attributes["id"].Value;
                var version = package.Attributes["version"].Value;
                var targetFramework = (package.Attributes["targetFramework"] != null ? package.Attributes["targetFramework"].Value : null) ?? "";

                var packagePath = Path.Combine(
                    this.m_RootPath,
                    "packages",
                    id + "." + version,
                    id + "." + version + ".nuspec");
                var packageDoc = new XmlDocument();
                packageDoc.Load(packagePath);

                // If the references are explicitly provided in the nuspec, use
                // those as to what files should be referenced by the projects.
                List<string> references = new List<string>();
                if (packageDoc
                    .DocumentElement
                    .FirstChild
                    .ChildNodes
                    .Cast<XmlElement>()
                    .Count(x => x.Name == "references") > 0)
                {
                    references = packageDoc.DocumentElement
                        .FirstChild
                        .ChildNodes
                        .Cast<XmlElement>()
                        .First(x => x.Name == "references")
                        .ChildNodes
                        .Cast<XmlElement>()
                        .Where(x => x.Name == "reference")
                        .Select(x => x.Attributes["file"].Value)
                        .ToList();
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
                    this.m_RootPath,
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
                            this.m_RootPath,
                            referenceBasePath,
                            clrName)))
                            continue;

                        // Otherwise enumerate through all of the libraries in this folder.
                        foreach (var dll in Directory.EnumerateFiles(
                            Path.Combine(
                            this.m_RootPath,
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
                                this.m_RootPath,
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
                            this.m_RootPath,
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

        private XmlDocument GenerateContentProject(XmlDocument source)
        {
            var allFiles = new Dictionary<string, IEnumerable<string>>();
            string sourceFile = null;
            string sourceFileFolder = null;
            foreach (var element in source
                .DocumentElement
                .ChildNodes
                .Cast<XmlElement>()
                .Where(x => x.Name == "Source"))
            {
                var sourceFolder = element.GetAttribute("Include");
                //Pattern matching to enable platform specific content
                if (sourceFolder.Contains("$(Platform)"))
                {
                    sourceFolder = sourceFolder.Replace("$(Platform)", m_Platform);
                }
                var matchFiles = element.GetAttribute("Match");
                var originalSourceFolder = sourceFolder;
                if (element.HasAttribute("Primary") && element.GetAttribute("Primary").ToLower() == "true")
                {
                    sourceFileFolder = Path.Combine(this.m_RootPath, sourceFolder);
                    sourceFile = Path.Combine(this.m_RootPath, sourceFolder, ".source");
                    using (var writer = new StreamWriter(sourceFile))
                    {
                        var dir = new DirectoryInfo(sourceFileFolder);
                        writer.Write(dir.FullName);
                    }
                }
                sourceFolder = Path.Combine(this.m_RootPath, sourceFolder);
                var files = this.GetListOfFilesInDirectory(sourceFolder, matchFiles);
                allFiles.Add(
                    originalSourceFolder,
                    files);
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
                    var relativePath = "Content\\" + file
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
                var relativePath = "Content\\" + sourceFile
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

        private XmlDocument CreateInputFor(string platform)
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
            foreach (var projectDoc in this.m_ProjectDocuments)
            {
                projects.AppendChild(doc.ImportNode(
                    projectDoc.DocumentElement,
                    true));
            }
            return doc;
        }
    }
}

