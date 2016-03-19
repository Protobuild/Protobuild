using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    internal class ExternalProjectReferenceResolver : IExternalProjectReferenceResolver
    {
        public void ResolveExternalProjectReferences(List<LoadedDefinitionInfo> documents, XmlDocument projectDoc, string targetPlatform)
        {
            var documentsByName = documents.ToDictionarySafe(
                k => k.Definition.Name,
                v => v,
                (dict, x) =>
                {
                    var existing = dict[x.Definition.Name];
                    var tried = x;

                    Console.WriteLine("WARNING: There is more than one project with the name " +
                                      x.Definition.Name + " (first project loaded from " + tried.Definition.AbsolutePath + ", " +
                                      "skipped loading second project from " + existing.Definition.AbsolutePath + ")");
                })
                .ToDictionary(k => k.Key, v => v.Value.Project);

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
                        var externalDocumentReferences = referencedDocument.SelectNodes("/ExternalProject/Reference").OfType<XmlElement>().Concat(
                            referencedDocument.SelectNodes("/ExternalProject/Platform[@Type='" + targetPlatform + "']/Reference").OfType<XmlElement>()).ToList();
                        foreach (var externalReference in externalDocumentReferences)
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

