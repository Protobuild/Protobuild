using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    public class ExternalProjectReferenceResolver : IExternalProjectReferenceResolver
    {
        public void ResolveExternalProjectReferences(List<XmlDocument> documents, XmlDocument projectDoc)
        {
            var documentsByName = documents.ToDictionary(k => k.DocumentElement.GetAttribute("Name"), v => v);

            var modified = true;
            while (modified)
            {
                modified = false;

                var currentProjectReferences = projectDoc.SelectNodes("/Project/References/Reference").OfType<XmlElement>()
                    .Select(k => k.GetAttribute("Include")).ToList();

                foreach (var reference in currentProjectReferences)
                {
                    if (!documentsByName.ContainsKey(reference))
                    {
                        continue;
                    }

                    var referencedDocument = documentsByName[reference];

                    if (referencedDocument.DocumentElement.LocalName == "ExternalProject")
                    {
                        // Find all top-level references in the external project.
                        var externalDocumentReferences = referencedDocument.SelectNodes("/ExternalProject/Reference");
                        foreach (var externalReference in externalDocumentReferences.OfType<XmlElement>())
                        {
                            var externalReferenceTarget = externalReference.GetAttribute("Include");

                            if (documentsByName.ContainsKey(externalReferenceTarget))
                            {
                                // Check to see if the reference points to an external project.
                                if (documentsByName[externalReferenceTarget].DocumentElement.LocalName == "ExternalProject")
                                {
                                    // Make sure we don't have a reference to it already.
                                    if (!currentProjectReferences.Contains(externalReferenceTarget))
                                    {
                                        var referencesNode = projectDoc.SelectSingleNode("/Project/References");
                                        var newReferenceNode = projectDoc.CreateElement("Reference");
                                        newReferenceNode.SetAttribute("Include", externalReferenceTarget);
                                        referencesNode.AppendChild(newReferenceNode);
                                        modified = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

