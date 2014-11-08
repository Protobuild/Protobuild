using System;
using System.Xml;
using System.Collections.Generic;
using Protobuild.Services;
using System.Linq;

namespace Protobuild
{
    public class ServiceReferenceTranslator : IServiceReferenceTranslator
    {
        public XmlNode TranslateProjectWithServiceReferences(XmlNode importNode, List<Service> services)
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
    }
}

