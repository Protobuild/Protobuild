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
    public class DefinitionSynchroniser
    {
        private DefinitionInfo m_DefinitionInfo;
        private CSharpProject m_CSharpProject;

        public DefinitionSynchroniser(DefinitionInfo info, CSharpProject project)
        {
            this.m_DefinitionInfo = info;
            this.m_CSharpProject = project;
        }

        public void Synchronise(string platform)
        {
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
            foreach (var file in files.ChildNodes.OfType<XmlElement>().ToArray())
            {
                var children = file.ChildNodes.OfType<XmlElement>().ToArray();
                if (children.Any(x => x.LocalName == "Platforms"))
                {
                    // This is a platform specific file.
                    var platforms = children.First(x => x.LocalName == "Platforms").InnerText;
                    if (platforms.Split(',').Contains(platform, StringComparer.OrdinalIgnoreCase))
                    {
                        files.RemoveChild(file);
                    }
                }
                else
                    files.RemoveChild(file);
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
                    "Protobuild.ProjectReader.WashNamespaces.xslt")))
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

