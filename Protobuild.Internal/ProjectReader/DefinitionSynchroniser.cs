using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Protobuild
{
    using System.Runtime.Remoting.Messaging;
    using Protobuild.Services;

    public class DefinitionSynchroniser
    {
        private readonly ModuleInfo m_ModuleInfo;

        private DefinitionInfo m_DefinitionInfo;
        private CSharpProject m_CSharpProject;

        public DefinitionSynchroniser(ModuleInfo moduleInfo, DefinitionInfo info, CSharpProject project)
        {
            this.m_ModuleInfo = moduleInfo;
            this.m_DefinitionInfo = info;
            this.m_CSharpProject = project;
        }

        public void Synchronise(string platform)
        {
            var serviceCache = Path.Combine(
                this.m_ModuleInfo.Path,
                this.m_ModuleInfo.Name + "." + platform + ".speccache");
            var ignoreServiceFiles = !File.Exists(serviceCache);
            var serviceManager = new ServiceManager(null);
            List<Service> services = null;
            if (!ignoreServiceFiles)
            {
                services = serviceManager.LoadServiceSpec(serviceCache);
            }

            var document = new XmlDocument();
            document.Load(this.m_DefinitionInfo.DefinitionPath);

            var projectElement = document.ChildNodes.OfType<XmlElement>()
                .FirstOrDefault(x => x.Name == "Project");
            var elements = projectElement.ChildNodes.OfType<XmlElement>().ToList();

            var files = elements.First(x => x.Name == "Files");

            // Remove files that either have no Platforms child, or where the
            // Platforms child contains the current platform that we're synchronising for.
            // This is because if I generate a platform for Linux, and the definition
            // has Windows-only files in it, those won't be in the project file.
            // Also for files that have services in them, check to see if we have the
            // service cache.  If we don't have a service cache, don't remove any entries
            // that are conditional on services (because we don't know what services 
            // were enabled when the project was generated).  Otherwise only allow removal
            // if the service was active at the time.
            foreach (var file in files.ChildNodes.OfType<XmlElement>().ToArray())
            {
                var children = file.ChildNodes.OfType<XmlElement>().ToArray();

                var platformsTag = children.FirstOrDefault(x => x.LocalName == "Platforms");
                var includePlatformsTag = children.FirstOrDefault(x => x.LocalName == "IncludePlatforms");
                var excludePlatformsTag = children.FirstOrDefault(x => x.LocalName == "ExcludePlatforms");

                var platformsTagString = platformsTag != null ? platformsTag.InnerText : string.Empty;
                var includePlatformsTagString = includePlatformsTag != null ? includePlatformsTag.InnerText : string.Empty;
                var excludePlatformsTagString = excludePlatformsTag != null ? excludePlatformsTag.InnerText : string.Empty;

                var servicesTag = children.FirstOrDefault(x => x.LocalName == "Services");
                var includeServicesTag = children.FirstOrDefault(x => x.LocalName == "IncludeServices");
                var excludeServicesTag = children.FirstOrDefault(x => x.LocalName == "ExcludeServices");

                var servicesTagString = servicesTag != null ? servicesTag.InnerText : string.Empty;
                var includeServicesTagString = includeServicesTag != null ? includeServicesTag.InnerText : string.Empty;
                var excludeServicesTagString = excludeServicesTag != null ? excludeServicesTag.InnerText : string.Empty;

                if (!string.IsNullOrEmpty(servicesTagString) || !string.IsNullOrEmpty(includeServicesTagString)
                    || !string.IsNullOrEmpty(excludeServicesTagString))
                {
                    if (ignoreServiceFiles)
                    {
                        // We don't know whether the service was enabled or not during generation, so we can't determine
                        // if the user wanted to remove this entry.
                        continue;
                    }

                    if (!string.IsNullOrEmpty(excludeServicesTagString))
                    {
                        var excluded = false;

                        foreach (var service in services)
                        {
                            if (excludeServicesTagString.Split(',').Contains(service.FullName, StringComparer.OrdinalIgnoreCase) ||
                                (service.ProjectName == this.m_DefinitionInfo.Name && excludeServicesTagString.Split(',').Contains(service.ServiceName, StringComparer.OrdinalIgnoreCase)))
                            {
                                // This file is excluded in the C# project for this platform
                                // and won't be present.
                                excluded = true;
                            }
                        }

                        if (excluded)
                        {
                            // This file is excluded in the C# project for this platform
                            // and won't be present.
                            continue;
                        }
                    }

                    // Fallback to <IncludeServices> if <Services> is not present.
                    if (string.IsNullOrEmpty(servicesTagString))
                    {
                        servicesTagString = includeServicesTagString;
                    }

                    var allowFallthrough = false;

                    // If both the <IncludeServices> and <Services> tags are not present, then this
                    // file is generated for all services regardless.
                    if (servicesTag == null && includeServicesTag == null)
                    {
                        allowFallthrough = true;
                    }
                    else
                    {
                        var included = false;

                        foreach (var service in services)
                        {
                            // If the included services string contains any enabled service, then we
                            // remove the file (because it will be present in the C# project).
                            if (servicesTagString.Split(',')
                                    .Contains(service.FullName, StringComparer.OrdinalIgnoreCase)
                                || (service.ProjectName == this.m_DefinitionInfo.Name
                                    && servicesTagString.Split(',')
                                           .Contains(service.ServiceName, StringComparer.OrdinalIgnoreCase)))
                            {
                                included = true;
                                break;
                            }
                        }

                        if (included)
                        {
                            // This file is included in the C# project for this service.
                            allowFallthrough = true;
                        }
                    }

                    if (!allowFallthrough)
                    {
                        // We weren't included with <Services> or <IncludeServices> so we won't
                        // be present regardless of platform settings.
                        continue;
                    }

                    // We were included by a service, but we might not have passed the platform check
                    // so continue performing checks.
                }

                if (!string.IsNullOrEmpty(excludePlatformsTagString))
                {
                    if (excludePlatformsTagString.Split(',').Contains(platform, StringComparer.OrdinalIgnoreCase))
                    {
                        // This file is excluded in the C# project for this platform
                        // and won't be present.
                        continue;
                    }
                }

                // Fallback to <IncludePlatforms> if <Platforms> is not present.
                if (string.IsNullOrEmpty(platformsTagString))
                {
                    platformsTagString = includePlatformsTagString;
                }

                // If both the <IncludePlatforms> and <Platforms> tags are not present, then this
                // file is generated for all platforms regardless.
                if (platformsTag == null && includePlatformsTag == null)
                {
                    files.RemoveChild(file);
                    continue;
                }

                // If the included platforms string contains the current platform, then we
                // remove the file (because it will be present in the C# project).
                if (platformsTagString.Split(',').Contains(platform, StringComparer.OrdinalIgnoreCase))
                {
                    files.RemoveChild(file);
                }
            }

            // Add the new files.
            var uniquePaths = new List<string>();
            foreach (var element in this.m_CSharpProject.Elements.OrderBy(x => x.Name).ThenBy(x => this.NormalizePath(x.GetAttribute("Include"))))
            {
                // Ignore Content files.
                if (element.Name == "None" || element.Name == "AndroidAsset")
                {
                    var linkElement = element.ChildNodes
                        .OfType<XmlNode>().FirstOrDefault(x => x.Name == "Link");
                    if (linkElement != null)
                    {
                        if (linkElement.InnerText.Trim().Replace('\\', '/').StartsWith("Content/", StringComparison.Ordinal))
                            continue;
                    }
                }

                var normalizedPath = this.NormalizePath(element.GetAttribute("Include"));

                // Ignore files that have already been added to the list.
                if (uniquePaths.Contains(normalizedPath))
                {
                    // Do not include again.
                    continue;
                }

                uniquePaths.Add(normalizedPath);

                // Change the path.
                element.SetAttribute("Include", normalizedPath);

                // Append the file element.
                files.AppendChild(document.ImportNode(element, true));
            }

            // Clean empty elements as well.
            var cleaned = this.WashNamespaces(document);
            foreach (var child in cleaned.ChildNodes.OfType<XmlElement>())
            {
                this.CleanNodes(child);
            }

            // Load into an XDocument to resort the list of elements by their Include.
            var xRoot = this.ToXDocument(cleaned);
            var xFiles = xRoot.Element(XName.Get("Project")).Element(XName.Get("Files"));
            var xOrderedNodes = xFiles.Elements()
                .OrderBy(x => x.Name.LocalName)
                .ThenBy(x => x.Attribute(XName.Get("Include")) == null ? "" : this.NormalizePath(x.Attribute(XName.Get("Include")).Value)).ToArray();
            xFiles.RemoveAll();
            foreach (var a in xOrderedNodes)
            {
                xFiles.Add(a);
            }
            cleaned = this.ToXmlDocument(xRoot);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                Encoding = Encoding.UTF8
            };
            using (var memory = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memory, settings))
                {
                    cleaned.Save(writer);
                }
                memory.Seek(0, SeekOrigin.Begin);
                var reader = new StreamReader(memory);
                var content = reader.ReadToEnd().Trim() + Environment.NewLine;
                using (var writer = new StreamWriter(this.m_DefinitionInfo.DefinitionPath, false, Encoding.UTF8))
                {
                    writer.Write(content);
                }
            }

            this.HandleNuGetConfig(platform);
        }

        /// <summary>
        /// Synchronises the package.config file back to the package.&lt;Platform&gt;.config file if it's present.
        /// </summary>
        /// <param name="platform"></param>
        private void HandleNuGetConfig(string platform)
        {
            var rootPath = this.m_DefinitionInfo.Path;

            if (File.Exists(Path.Combine(rootPath, "packages." + platform + ".config")))
            {
                if (File.Exists(Path.Combine(rootPath, "packages.config")))
                {
                    File.Copy(
                        Path.Combine(rootPath, "packages.config"),
                        Path.Combine(rootPath, "packages." + platform + ".config"),
                        true);
                }
            }
        }

        private string NormalizePath(string path)
        {
            return path.Replace('/', '\\');
        }

        private XslCompiledTransform GetCompiledTransform()
        {
            var resolver = new EmbeddedResourceResolver();
            var transform = new XslCompiledTransform();
            using (var reader = XmlReader.Create(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "WashNamespaces.xslt")))
            {
                transform.Load(
                    reader,
                    XsltSettings.TrustedXslt,
                    resolver
                );
            }
            return transform;
        }

        private XmlDocument WashNamespaces(XmlDocument input)
        {
            using (var memory = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(memory))
                {
                    this.GetCompiledTransform().Transform(input, writer);
                }
                memory.Seek(0, SeekOrigin.Begin);
                using (var reader = XmlReader.Create(memory))
                {
                    var document = new XmlDocument();
                    document.Load(reader);
                    return document;
                }
            }
        }

        private void CleanNodes(XmlElement node)
        {
            foreach (var child in node.ChildNodes.OfType<XmlElement>())
            {
                this.CleanNodes(child);
            }
            if (string.IsNullOrWhiteSpace(node.InnerXml))
                node.IsEmpty = true;
        }

        private XmlDocument ToXmlDocument(XDocument xDocument)
        {
            var xmlDocument = new XmlDocument();
            using(var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }

        private XDocument ToXDocument(XmlDocument xmlDocument)
        {
            using (var nodeReader = new XmlNodeReader(xmlDocument))
            {
                nodeReader.MoveToContent();
                return XDocument.Load(nodeReader);
            }
        }
    }
}

