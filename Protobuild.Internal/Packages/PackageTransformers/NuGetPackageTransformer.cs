using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;

namespace Protobuild
{
    public class NuGetPackageTransformer : IPackageTransformer
    {
        private readonly IProgressiveWebOperation _progressiveWebOperation;

        private readonly IPackageCreator _packageCreator;

        private readonly IGetRecursiveUtilitiesInPath _getRecursiveUtilitiesInPath;

        public NuGetPackageTransformer(
            IProgressiveWebOperation progressiveWebOperation,
            IPackageCreator packageCreator,
            IGetRecursiveUtilitiesInPath getRecursiveUtilitiesInPath)
        {
            _progressiveWebOperation = progressiveWebOperation;
            _packageCreator = packageCreator;
            _getRecursiveUtilitiesInPath = getRecursiveUtilitiesInPath;
        }

        public byte[] Transform(string url, string gitReference, string platform, out string format)
        {
            var urlAndPackageName = url.Split(new[] { '|' }, 2);

            var repoUrl = urlAndPackageName[0];
            var packageName = urlAndPackageName[1];

            var packagesUrl = repoUrl.TrimEnd('/') + "/Packages(Id='" + packageName + "',Version='" + gitReference + "')";
            var client = new WebClient();

            Console.WriteLine("HTTP GET " + packagesUrl);
            var xmlString = client.DownloadString(packagesUrl);
            var xDocument = XDocument.Parse(xmlString);

            Console.WriteLine("Retrieved package information");
            var title = xDocument.Root.Elements().First(x => x.Name.LocalName == "title");
            var content = xDocument.Root.Elements().First(x => x.Name.LocalName == "content");

            Console.WriteLine("Found NuGet package '" + title.Value + "'");

            var downloadUrl = content.Attributes().First(x => x.Name.LocalName == "src").Value;
            var downloadedZipData = _progressiveWebOperation.Get(downloadUrl);

            // Save the ZIP file onto disk.
            var tempFile = Path.GetTempFileName();
            using (var writer = new FileStream(tempFile, FileMode.Truncate, FileAccess.Write))
            {
                writer.Write(downloadedZipData, 0, downloadedZipData.Length);
            }

            // TODO: Add a hash of the package URL and version into this path.
            var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);

            Console.WriteLine("Extracting package to " + tempFolder + "...");

            // We aren't running .NET 4.5, we can't refer to external DLLs and there's no
            // freely licensed, embeddable C# ZIP extraction code that we can include (like
            // the LZMA / TAR code).  So instead, we need to jump out to an external process
            // to actually extract the package, and hope that the user has the appropriate
            // utilities installed on their system.
            if (Path.DirectorySeparatorChar == '/')
            {
                // On UNIX, use the "unzip" utility to extract the package into a temporary
                // folder.
                var startInfo = new ProcessStartInfo
                {
                    FileName = "unzip",
                    Arguments = "\"" + tempFile + "\" -d \"" + tempFolder + "\"",
                };
                var p = Process.Start(startInfo);
                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "Unable to extract NuGet package using 'unzip' " + 
                        "utility.  Is it installed on your system?");
                }
            }
            else
            {
                throw new NotSupportedException("TODO: Figure out ZIP extraction on Windows");
            }

            Console.WriteLine("Extraction complete.");

            Console.WriteLine("Auto-detecting libraries to reference from NuGet package...");

            var packagePath = new DirectoryInfo(tempFolder).GetFiles("*.nuspec").First().FullName;
            var libraryReferences = new Dictionary<string, string>();

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
                // Exact matches
                "=net40-client",
                "=Net40-client",
                "=net40",
                "=Net40",
                "=net35",
                "=Net35",
                "=net20",
                "=Net20",
                "=20",
                "=11",
                "=",

                // Substring matches
                "?net4",
                "?MonoAndroid",
            };

            // Determine the base path for all references; that is, the lib/ folder.
            var referenceBasePath = Path.Combine(
                tempFolder,
                "lib");

            // If we don't have a lib/ folder, then we aren't able to reference anything
            // anyway (this might be a tools only package like xunit.runners).
            if (!Directory.Exists(referenceBasePath))
            {
                Console.WriteLine(
                    "No lib/ folder found in downloaded NuGet " + 
                    "package.  There is nothing to reference.");
            }
            else
            {
                // If no references are in nuspec, reference all of the libraries that
                // are on disk.
                if (references.Count == 0)
                {
                    // Search through all of the target frameworks until we find one that
                    // has at least one file in it.
                    foreach (var clrNameOriginal in clrNames)
                    {
                        var clrName = clrNameOriginal;
                        var foundClr = false;

                        if (clrName[0] == '=')
                        {
                            // Exact match (strip the equals).
                            clrName = clrName.Substring(1);

                            // If this target framework doesn't exist for this library, skip it.
                            var dirPath = Path.Combine(
                                referenceBasePath,
                                clrName);
                            if (!Directory.Exists(dirPath))
                            {
                                continue;
                            }
                        }
                        else if (clrName[0] == '?')
                        {
                            // Substring, search the reference base path for any folders
                            // with a matching substring.
                            clrName = clrName.Substring(1);

                            var baseDirPath = referenceBasePath;
                            var found = false;
                            foreach (var subdir in new DirectoryInfo(baseDirPath).GetDirectories())
                            {
                                if (subdir.Name.Contains(clrName))
                                {
                                    clrName = subdir.Name;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown CLR name match type with '" + clrName + "'");
                        }

                        // Otherwise enumerate through all of the libraries in this folder.
                        foreach (var dll in Directory.EnumerateFiles(
                            Path.Combine(
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
                                    packageDll)))
                            {
                                // Create the library reference.
                                libraryReferences.Add(
                                    Path.GetFileNameWithoutExtension(dll),
                                    packageDll);

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
                                packageDll)))
                        {
                            libraryReferences.Add(
                                Path.GetFileNameWithoutExtension(packageDll),
                                packageDll);
                            break;
                        }
                    }
                }
            }

            foreach (var kv in libraryReferences)
            {
                Console.WriteLine("Found library to reference: " + kv.Key + " (at " + kv.Value + ")");
            }

            Console.WriteLine("Generating external project reference...");
            var document = new XmlDocument();
            var externalProject = document.CreateElement("ExternalProject");
            externalProject.SetAttribute("Name", packageName);
            document.AppendChild(externalProject);
            foreach (var kv in libraryReferences)
            {
                var binaryReference = document.CreateElement("Binary");
                binaryReference.SetAttribute("Name", kv.Key);
                binaryReference.SetAttribute("Path", kv.Value.Substring(tempFolder.Length).TrimStart(new[] { '/', '\\' }));
                externalProject.AppendChild(binaryReference);
            }
            document.Save(Path.Combine(tempFolder, "_ProtobuildExternalProject.xml"));

            Console.WriteLine("Generating module...");
            var generatedModule = new ModuleInfo();
            generatedModule.Name = packageName;
            generatedModule.Save(Path.Combine(tempFolder, "_ProtobuildModule.xml"));

            Console.WriteLine("Converting to a Protobuild package...");

            var target = new MemoryStream();
            var filter = new FileFilter(_getRecursiveUtilitiesInPath.GetRecursiveFilesInPath(tempFolder));

            foreach (var kv in libraryReferences)
            {
                filter.ApplyInclude(Regex.Escape(kv.Value.Substring(tempFolder.Length).TrimStart(new[] { '/', '\\' })));
            }

            filter.ApplyInclude("_ProtobuildExternalProject\\.xml");
            filter.ApplyRewrite("_ProtobuildExternalProject\\.xml", "Build/Projects/" + packageName + ".definition");
            filter.ApplyInclude("_ProtobuildModule\\.xml");
            filter.ApplyRewrite("_ProtobuildModule\\.xml", "Build/Module.xml");

            filter.ImplyDirectories();

            _packageCreator.Create(
                target,
                filter,
                tempFolder,
                PackageManager.ARCHIVE_FORMAT_TAR_LZMA);
            format = PackageManager.ARCHIVE_FORMAT_TAR_LZMA;

            Console.WriteLine("Cleaning up temporary files...");
            Directory.Delete(tempFolder, true);

            Console.WriteLine("Package conversion complete.");
            var bytes = new byte[target.Position];
            target.Seek(0, SeekOrigin.Begin);
            target.Read(bytes, 0, bytes.Length);

            return bytes;
        }
    }
}

