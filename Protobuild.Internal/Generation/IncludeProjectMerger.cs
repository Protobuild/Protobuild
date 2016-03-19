using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace Protobuild
{
    internal class IncludeProjectMerger : IIncludeProjectMerger
    {
        public void MergeInReferencesAndPropertiesForIncludeProjects(List<LoadedDefinitionInfo> documents, XmlDocument projectDoc, string targetPlatform)
        {
            // While the <Files> section is handled explicitly in the XSLT (so that
            // synchronisation behaves correctly), the <References> and <Properties>
            // sections are handled here, because supporting them via XSLT would
            // make the XSLT drastically more complex.

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

            var currentProjectReferences = projectDoc.SelectNodes("/Project/References/Reference").OfType<XmlElement>()
                .Select(k => k.GetAttribute("Include")).ToList();

            var referencesToAdd = new List<XmlNode>();
            var propertiesToAdd = new List<XmlNode>();

            foreach (var reference in currentProjectReferences)
            {
                if (!documentsByName.ContainsKey(reference))
                {
                    continue;
                }

                var referencedDocument = documentsByName[reference];

                if (referencedDocument.DocumentElement.LocalName == "IncludeProject")
                {
                    // Find references and copy them in.
                    var includeProjectReferences = referencedDocument.SelectNodes("/IncludeProject/References/Reference").OfType<XmlElement>().ToList();
                    foreach (var @ref in includeProjectReferences)
                    {
                        referencesToAdd.Add(projectDoc.ImportNode(@ref, true));
                    }

                    // Find all nodes under the <Properties> section and deep-copy them in.
                    var includeProjectProperties = referencedDocument.SelectNodes("/IncludeProject/Properties/*").OfType<XmlElement>().ToList();
                    foreach (var node in includeProjectProperties)
                    {
                        propertiesToAdd.Add(projectDoc.ImportNode(node, true));
                    }
                }
            }

            var references = projectDoc.DocumentElement.SelectSingleNode("References");
            if (references == null)
            {
                references = projectDoc.CreateElement("References");
                projectDoc.DocumentElement.AppendChild(references);
            }

            foreach (var @ref in referencesToAdd)
            {
                references.AppendChild(@ref);
            }

            var properties = projectDoc.DocumentElement.SelectSingleNode("Properties");
            if (properties == null)
            {
                properties = projectDoc.CreateElement("Properties");
                projectDoc.DocumentElement.AppendChild(properties);
            }

            foreach (var node in propertiesToAdd)
            {
                properties.AppendChild(node);
            }
        }
    }
}

