using System;
using System.Xml;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace Protobuild
{
    public class NuGetReferenceDetector : INuGetReferenceDetector
    {
        public void ApplyNuGetReferences(
            string rootPath,
            string packagesPath,
            XmlDocument document,
            XmlNode nuget)
        {
            // Read the packages document and generate Project nodes for
            // each package that we want.
            var packagesDoc = new XmlDocument();
            packagesDoc.Load(packagesPath);
            var packages = packagesDoc.DocumentElement
                .ChildNodes
                .OfType<XmlElement>()
                .Where(x => x.Name == "package")
                .Select(x => x);
            foreach (var package in packages)
            {
                var id = package.Attributes["id"].Value;
                var version = package.Attributes["version"].Value;
                var targetFramework = (package.Attributes["targetFramework"] != null ? package.Attributes["targetFramework"].Value : null) ?? "";

                var packagePath = Path.Combine(
                    rootPath,
                    "packages",
                    id + "." + version,
                    id + "." + version + ".nuspec");

                // Use the nuspec file if it exists.
                List<string> references = new List<string>();
                if (File.Exists(packagePath))
                {
                    var packageDoc = new XmlDocument();
                    packageDoc.Load(packagePath);

                    // If the references are explicitly provided in the nuspec, use
                    // those as to what files should be referenced by the projects.
                    if (
                        packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
                        .Count(x => x.Name == "references") > 0)
                    {
                        references =
                            packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
                                .First(x => x.Name == "references")
                                .ChildNodes.OfType<XmlElement>()
                                .Where(x => x.Name == "reference")
                                .Select(x => x.Attributes["file"].Value)
                                .ToList();
                    }
                }

                // Determine the priority of the frameworks that we want to target
                // out of the available versions.
                string[] clrNames = new[]
                {
                    targetFramework,
                    "net40-client",
                    "Net40-client",
                    "net40",
                    "Net40",
                    "net35",
                    "Net35",
                    "net20",
                    "Net20",
                    "20",
                    "11",
                    ""
                };

                // Determine the base path for all references; that is, the lib/ folder.
                var referenceBasePath = Path.Combine(
                    "packages",
                    id + "." + version,
                    "lib");

                // If we don't have a lib/ folder, then we aren't able to reference anything
                // anyway (this might be a tools only package like xunit.runners).
                if (!Directory.Exists(
                    Path.Combine(
                        rootPath,
                        referenceBasePath)))
                    continue;

                // If no references are in nuspec, reference all of the libraries that
                // are on disk.
                if (references.Count == 0)
                {
                    // Search through all of the target frameworks until we find one that
                    // has at least one file in it.
                    foreach (var clrName in clrNames)
                    {
                        var foundClr = false;

                        // If this target framework doesn't exist for this library, skip it.
                        if (!Directory.Exists(
                            Path.Combine(
                                rootPath,
                                referenceBasePath,
                                clrName)))
                            continue;

                        // Otherwise enumerate through all of the libraries in this folder.
                        foreach (var dll in Directory.EnumerateFiles(
                            Path.Combine(
                                rootPath,
                                referenceBasePath, clrName),
                            "*.dll"))
                        {
                            // Determine the relative path to the library.
                            var packageDll = Path.Combine(
                                referenceBasePath,
                                clrName,
                                Path.GetFileName(dll));

                            // Confirm again that the file actually exists on disk when
                            // combined with the root path.
                            if (File.Exists(
                                Path.Combine(
                                    rootPath,
                                    packageDll)))
                            {
                                // Create the library reference.
                                var packageReference =
                                    document.CreateElement("Package");
                                packageReference.SetAttribute(
                                    "Name",
                                    Path.GetFileNameWithoutExtension(dll));
                                packageReference.AppendChild(
                                    document.CreateTextNode(packageDll
                                        .Replace('/', '\\')));
                                nuget.AppendChild(packageReference);

                                // Mark this target framework as having provided at least
                                // one reference.
                                foundClr = true;
                            }
                        }

                        // Break if we have found at least one reference.
                        if (foundClr)
                            break;
                    }
                }

                // For all of the references that were found in the original nuspec file,
                // add those references.
                foreach (var reference in references)
                {
                    // Search through all of the target frameworks until we find the one
                    // that has the reference in it.
                    foreach (var clrName in clrNames)
                    {
                        // If this target framework doesn't exist for this library, skip it.
                        var packageDll = Path.Combine(
                            referenceBasePath,
                            clrName,
                            reference);

                        if (File.Exists(
                            Path.Combine(
                                rootPath,
                                packageDll)))
                        {
                            // Create the library reference.
                            var packageReference =
                                document.CreateElement("Package");
                            packageReference.SetAttribute(
                                "Name",
                                id);
                            packageReference.AppendChild(
                                document.CreateTextNode(packageDll
                                    .Replace('/', '\\')));
                            nuget.AppendChild(packageReference);
                            break;
                        }
                    }
                }


            }
        }
    }
}

