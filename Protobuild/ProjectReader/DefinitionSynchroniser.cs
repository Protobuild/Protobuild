using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Xsl;

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
            
            var projectElement = document.ChildNodes.Cast<XmlNode>()
                .Where(x => x is XmlElement).Cast<XmlElement>()
                .FirstOrDefault(x => x.Name == "Project");
            var elements = projectElement.ChildNodes.Cast<XmlNode>()
                .Where(x => x is XmlElement).Cast<XmlElement>().ToList();
            
            var files = elements.Cast<XmlElement>().First(x => x.Name == "Files");
            
            // Remove files that either have no Platforms child, or where the
            // Platforms child contains the current platform that we're synchronising for.
            // This is because if I generate a platform for Linux, and the definition
            // has Windows-only files in it, those won't be in the project file.
            foreach (var file in files.ChildNodes.Cast<XmlNode>().Where(x => x is XmlElement).Cast<XmlElement>().ToArray())
            {
                var children = file.ChildNodes.Cast<XmlNode>().Where(x => x is XmlElement).Cast<XmlElement>().ToArray();
                if (children.Any(x => x.LocalName == "Platforms"))
                {
                    // This is a platform specific file.
                    var platforms = children.First().InnerText;
                    if (platforms.Split(',').Contains(platform, StringComparer.OrdinalIgnoreCase))
                        files.RemoveChild(file);
                }
                else
                    files.RemoveChild(file);
            }
            
            // Add the new files.
            foreach (var element in this.m_CSharpProject.Elements.OrderBy(x => x.Name).ThenBy(x => x.GetAttribute("Include")))
            {
                // Ignore Content files.
                if (element.Name == "None")
                {
                    var linkElement = element.ChildNodes
                        .Cast<XmlNode>().FirstOrDefault(x => x.Name == "Link");
                    if (linkElement != null)
                    {
                        if (linkElement.InnerText.Trim().Replace('\\', '/').StartsWith("Content/", StringComparison.Ordinal))
                            continue;
                    }
                }
                
                // Append the file element.
                files.AppendChild(document.ImportNode(element, true));
            }
            
            // Clean empty elements as well.
            var cleaned = this.WashNamespaces(document);
            foreach (var child in cleaned.ChildNodes.Cast<XmlNode>().Where(x => x is XmlElement))
            {
                this.CleanNodes((XmlElement)child);
            }
            
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n"
            };
            using (var writer = XmlWriter.Create(this.m_DefinitionInfo.DefinitionPath, settings))
            {
                cleaned.Save(writer);
            }
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
            foreach (var child in node.ChildNodes.Cast<XmlNode>().Where<XmlNode>(x => x is XmlElement))
            {
                this.CleanNodes((XmlElement)child);
            }
            if (string.IsNullOrWhiteSpace(node.InnerXml))
                node.IsEmpty = true;
        }
    }
}

