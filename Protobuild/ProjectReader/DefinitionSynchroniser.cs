using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

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
            foreach (var reference in references.ChildNodes.Cast<XmlElement>().Select(x => x.InnerText.Trim()))
                if (!existingReferences.Contains(reference))
                    existingReferences.Add(reference);
            foreach (var reference in this.m_CSharpProject.References)
                if (!existingReferences.Contains(reference))
                    existingReferences.Add(reference);
            
            // Remove all existing references.
            references.RemoveAll();
            
            // Add the new ones.
            foreach (var reference in existingReferences)
            {
                var element = document.CreateElement("Reference");
                element.SetAttribute("Include", reference);
                references.AppendChild(element);
            }
            
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  "
            };
            using (var writer = XmlWriter.Create(this.m_DefinitionInfo.DefinitionPath, settings))
            {
                document.Save(writer);
            }
        }
    }
}

