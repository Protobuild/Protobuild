using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace Protobuild
{
    internal class IncludeProjectAppliesToUpdater : IIncludeProjectAppliesToUpdater
    {
        public void UpdateProjectReferences(List<XmlDocument> documents, XmlDocument projectDoc)
        {
            var currentName = projectDoc.DocumentElement.GetAttribute("Name");

            foreach (var doc in documents)
            {
                if (doc.DocumentElement.LocalName == "IncludeProject")
                {
                    var appliesTo = doc.DocumentElement.GetAttribute("AppliesTo").Split(',');
                    if (appliesTo.Contains(currentName))
                    {
                        var references = projectDoc.DocumentElement.SelectSingleNode("References");
                        var reference = projectDoc.CreateElement("Reference");
                        reference.SetAttribute("Include", doc.DocumentElement.GetAttribute("Name"));
                        references.AppendChild(reference);

                        Console.WriteLine(
                            "NOTE: Added " + doc.DocumentElement.GetAttribute("Name") + " as a reference in " + 
                            currentName + " because of an AppliesTo attribute on the include project.");
                    }
                }
            }
        }
    }
}

