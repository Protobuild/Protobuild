using System;
using System.Xml;
using System.Collections.Generic;
using Protobuild.Services;
using System.Linq;
using System.IO;

namespace Protobuild
{
    internal class ProjectInputGenerator : IProjectInputGenerator
    {
        private readonly IHostPlatformDetector m_HostPlatformDetector;

        private readonly IServiceInputGenerator m_ServiceInputGenerator;

        private readonly INuGetReferenceDetector m_NuGetReferenceDetector;

        private readonly IServiceReferenceTranslator m_ServiceReferenceTranslator;

        private readonly IJSILProvider m_JSILProvider;

        private readonly IFeatureManager _featureManager;

        public ProjectInputGenerator(
            IHostPlatformDetector hostPlatformDetector,
            IServiceInputGenerator serviceInputGenerator,
            INuGetReferenceDetector nuGetReferenceDetector,
            IServiceReferenceTranslator serviceReferenceTranslator,
            IJSILProvider jsilProvider,
            IFeatureManager featureManager)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_ServiceInputGenerator = serviceInputGenerator;
            this.m_NuGetReferenceDetector = nuGetReferenceDetector;
            this.m_ServiceReferenceTranslator = serviceReferenceTranslator;
            this.m_JSILProvider = jsilProvider;
            _featureManager = featureManager;
        }

        public XmlDocument Generate(
            List<XmlDocument> definitions,
            string rootPath,
            string projectName,
            string platformName,
            string packagesPath,
            IEnumerable<XmlElement> properties,
            List<Service> services)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var input = doc.CreateElement("Input");
            doc.AppendChild(input);

            input.AppendChild(this.m_ServiceInputGenerator.Generate(doc, projectName, services));

            var generation = doc.CreateElement("Generation");
            var projectNameNode = doc.CreateElement("ProjectName");
            projectNameNode.AppendChild(doc.CreateTextNode(projectName));
            var platformNameNode = doc.CreateElement("Platform");
            platformNameNode.AppendChild(doc.CreateTextNode(platformName));
            var hostPlatformName = doc.CreateElement("HostPlatform");
            hostPlatformName.AppendChild(doc.CreateTextNode(this.m_HostPlatformDetector.DetectPlatform()));

            if (string.Compare(platformName, "Web", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // Add JSIL properties
                string jsilDirectory, jsilCompilerFile;
                if (!this.m_JSILProvider.GetJSIL(out jsilDirectory, out jsilCompilerFile))
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

                foreach (var entry in this.m_JSILProvider.GetJSILLibraries())
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
                    definitions.Select(x => x.DocumentElement)
                        .Where(x => x.Attributes != null)
                        .Where(x => x.Attributes["Name"] != null)
                        .FirstOrDefault(x => x.Attributes["Name"].Value == projectName);
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
                            var srcDir = Path.Combine(rootPath, path);
                            if (Directory.Exists(srcDir))
                            {
                                if (!File.Exists(Path.Combine(srcDir, "index.htm")))
                                {
                                    Console.WriteLine("Extracting JSIL HTML template...");
                                    ResourceExtractor.ExtractJSILTemplate(projectName, Path.Combine(srcDir, "index.htm"));
                                }
                            }
                        }
                    }
                }
            }

            var rootName = doc.CreateElement("RootPath");
            rootName.AppendChild(doc.CreateTextNode(
                new DirectoryInfo(rootPath).FullName));
            generation.AppendChild(projectNameNode);
            generation.AppendChild(platformNameNode);
            generation.AppendChild(hostPlatformName);
            generation.AppendChild(rootName);
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

            var featuresNode = doc.CreateElement("Features");
            foreach (var feature in _featureManager.GetAllEnabledFeatures())
            {
                var featureNode = doc.CreateElement(feature.ToString());
                featureNode.AppendChild(doc.CreateTextNode("True"));
                featuresNode.AppendChild(featureNode);
            }
            input.AppendChild(featuresNode);

            var nuget = doc.CreateElement("NuGet");
            input.AppendChild(nuget);

            var projects = doc.CreateElement("Projects");
            input.AppendChild(projects);
            foreach (var projectDoc in definitions)
            {
                var importedProject = this.m_ServiceReferenceTranslator.TranslateProjectWithServiceReferences(
                    doc.ImportNode(projectDoc.DocumentElement, true), 
                    services);

                // Convert <Property> tags in projects other than the one we're currently
                // generating, so that we can lookup properties in other projects in the XSLT.
                var importedProjectProperties = importedProject.ChildNodes
                    .OfType<XmlElement>()
                    .FirstOrDefault(x => x.Name.ToLower() == "properties");
                if (importedProjectProperties != null)
                {
                    var existingProperties = importedProjectProperties.ChildNodes
                        .OfType<XmlElement>().ToList();
                    foreach (var property in existingProperties)
                    {
                        if (property.Name.ToLower() == "property")
                        {
                            if (property.GetAttribute("Name") == null)
                            {
                                throw new Exception(
                                    "A property is missing the Name attribute in the '" + 
                                    projectDoc.DocumentElement.GetAttribute("Name") + 
                                    "' project.");
                            }

                            var nodeName = doc.CreateElement(property.GetAttribute("Name"));
                            nodeName.AppendChild(doc.CreateTextNode(
                                property.GetAttribute("Value")));
                            importedProjectProperties.AppendChild(nodeName);
                        }
                    }
                }

                projects.AppendChild(importedProject);
            }

            // Also check if there are NuGet packages.config file for
            // this project and if there is, include all of the relevant
            // NuGet package information for referencing the correct DLLs.
            if (File.Exists(packagesPath))
            {
                this.m_NuGetReferenceDetector.ApplyNuGetReferences(
                    rootPath,
                    packagesPath,
                    doc,
                    nuget);
            }

            return doc;
        }
    }
}

