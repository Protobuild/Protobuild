using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Xsl;
using System.Reflection;
using System.IO;

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
        
        public void Synchronise()
        {
            var document = new XmlDocument();
            document.Load(this.m_DefinitionInfo.DefinitionPath);
            
            var projectElement = document.ChildNodes.Cast<XmlNode>()
                .Where(x => x is XmlElement).Cast<XmlElement>()
                .FirstOrDefault(x => x.Name == "Project");
            var elements = projectElement.ChildNodes.Cast<XmlNode>()
                .Where(x => x is XmlElement).Cast<XmlElement>().ToList();
            
            var references = elements.Cast<XmlElement>().First(x => x.Name == "References");
            var files = elements.Cast<XmlElement>().First(x => x.Name == "Files");
            
            var existingReferences = new List<string>();
            foreach (var reference in references.ChildNodes.Cast<XmlElement>().Select(x => x.GetAttribute("Include").Trim()))
                if (!existingReferences.Contains(reference))
                    existingReferences.Add(reference);
            foreach (var reference in this.m_CSharpProject.References)
                if (!existingReferences.Contains(reference))
                    existingReferences.Add(reference);
            
            // Remove all existing references and files.
            references.RemoveAll();
            files.RemoveAll();
            
            // Add the new references.
            foreach (var reference in existingReferences)
            {
                var element = document.CreateElement("Reference");
                element.SetAttribute("Include", reference);
                references.AppendChild(element);
            }
            
            // Add the new files.
            foreach (var element in this.m_CSharpProject.Elements)
            {
                files.AppendChild(document.ImportNode(element, true));
            }
            
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  "
            };
            using (var writer = XmlWriter.Create(this.m_DefinitionInfo.DefinitionPath, settings))
            {
                this.WashNamespaces(document).Save(writer);
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
    }
}

