using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO.Compression;
using System.Xml;
using System.Text.RegularExpressions;

namespace Protobuild
{
    internal class NuGetPackageTransformer : IPackageTransformer
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

        private void CopyFolder(DirectoryInfo source, DirectoryInfo destination)
        {
            foreach (var subfolder in source.GetDirectories())
            {
                Directory.CreateDirectory(Path.Combine(destination.FullName, subfolder.Name));
                CopyFolder(
                    new DirectoryInfo(Path.Combine(source.FullName, subfolder.Name)),
                    new DirectoryInfo(Path.Combine(destination.FullName, subfolder.Name)));
            }

            foreach (var file in source.GetFiles())
            {
                File.Copy(
                    file.FullName,
                    Path.Combine(destination.FullName, file.Name));
            }
        }

        public byte[] Transform(string url, string gitReference, string platform, string format)
        {
            var urlAndPackageName = url.Split(new[] { '|' }, 2);

            if (urlAndPackageName.Length != 2)
            {
                Console.Error.WriteLine(
                    "ERROR: Malformed NuGet package reference '" + url +
                    "'.  Make sure you split the NuGet server URL and the package name with a pipe character (|).");
                ExecEnvironment.Exit(1);
            }

            var repoUrl = urlAndPackageName[0];
            var packageName = urlAndPackageName[1];

            var originalFolder = DownloadOrUseExistingNuGetPackage(repoUrl.TrimEnd('/'), packageName, gitReference);

            var folder = Path.GetTempFileName();
            File.Delete(folder);
            Directory.CreateDirectory(folder);

            byte[] bytes;
            try
            {
                Console.WriteLine("Copying directory for package transformation...");
                CopyFolder(new DirectoryInfo(originalFolder), new DirectoryInfo(folder));

                Console.WriteLine("Auto-detecting libraries to reference from NuGet package...");

                var packagePath = new DirectoryInfo(folder).GetFiles("*.nuspec").First().FullName;
                var libraryReferences = new Dictionary<string, string>();
                var packageDependencies = new Dictionary<string, string>();

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

                    // If there are dependencies specified, store them and convert them to
                    // Protobuild references, and reference them in the Module.xml file.
                    if (
                        packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
                            .Count(x => x.Name == "dependencies") > 0)
                    {
                        packageDependencies =
                            packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
                                .First(x => x.Name == "dependencies")
                                .ChildNodes.OfType<XmlElement>()
                                .Where(x => x.Name == "dependency")
                                .ToDictionarySafe(
                                    k => k.Attributes["id"].Value,
                                    v => v.Attributes["version"].Value,
                                    (dict, c) =>
                                        Console.WriteLine("WARNING: More than one dependency on " + c +
                                                          " in NuGet package."));
                    }
                }

                // Determine the priority of the frameworks that we want to target
                // out of the available versions.
                string[] clrNames = new[]
                {
                    // Exact matches
                    "=net45",
                    "=Net45",
                    "=net40-client",
                    "=Net40-client",
                    "=net403",
                    "=Net403",
                    "=net40",
                    "=Net40",
                    "=net35-client",
                    "=Net35-client",
                    "=net20",
                    "=Net20",
                    "=net11",
                    "=Net11",
                    "=20",
                    "=11",
                    "=",

                    // Substring matches
                    "?net45",
                    "?Net45",
                    "?net4",
                    "?Net4",
                    "?MonoAndroid",
                };

                if (platform == "WindowsUniversal")
                {
                    // This is the priority list for Windows Universal Apps.
                    clrNames = new[]
                    {
                        "=uap10.0",
                        "=uap",
                        "=netcore451",
                        "=netcore",
                        "=dotnet"
                    };
                }

                var referenceDirectories = new string[] {"ref", "lib"};

                foreach (var directory in referenceDirectories)
                {
                    // Determine the base path for all references; that is, the lib/ folder.
                    var referenceBasePath = Path.Combine(
                        folder,
                        directory);

                    if (Directory.Exists(referenceBasePath))
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
                                    throw new InvalidOperationException("Unknown CLR name match type with '" + clrName +
                                                                        "'");
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
                                        if (!libraryReferences.ContainsKey(Path.GetFileNameWithoutExtension(dll)))
                                        {
                                            libraryReferences.Add(
                                                Path.GetFileNameWithoutExtension(dll),
                                                packageDll);
                                        }

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
                                    if (!libraryReferences.ContainsKey(Path.GetFileNameWithoutExtension(packageDll)))
                                    {
                                        libraryReferences.Add(
                                            Path.GetFileNameWithoutExtension(packageDll),
                                            packageDll);
                                    }
                                    break;
                                }
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
                    binaryReference.SetAttribute("Path",
                        kv.Value.Substring(folder.Length).TrimStart(new[] {'/', '\\'}).Replace("%2B", "-"));
                    externalProject.AppendChild(binaryReference);
                }
                foreach (var package in packageDependencies)
                {
                    var externalReference = document.CreateElement("Reference");
                    externalReference.SetAttribute("Include", package.Key);
                    externalProject.AppendChild(externalReference);
                }
                document.Save(Path.Combine(folder, "_ProtobuildExternalProject.xml"));

                Console.WriteLine("Generating module...");
                var generatedModule = new ModuleInfo();
                generatedModule.Name = packageName;
                generatedModule.Packages = new List<PackageRef>();

                foreach (var package in packageDependencies)
                {
                    generatedModule.Packages.Add(new PackageRef
                    {
                        Uri =
                            repoUrl.Replace("http://", "http-nuget://").Replace("https://", "https-nuget://") + "|" +
                            package.Key,
                        GitRef = package.Value.TrimStart('[').TrimEnd(']'),
                        Folder = package.Key
                    });
                }

                generatedModule.Save(Path.Combine(folder, "_ProtobuildModule.xml"));

                Console.WriteLine("Converting to a Protobuild package...");

                var target = new MemoryStream();
                var filter = new FileFilter(_getRecursiveUtilitiesInPath.GetRecursiveFilesInPath(folder));

                foreach (var kv in libraryReferences)
                {
                    filter.ApplyInclude(
                        Regex.Escape(kv.Value.Substring(folder.Length).Replace('\\', '/').TrimStart('/')));
                    filter.ApplyRewrite(
                        Regex.Escape(kv.Value.Substring(folder.Length).Replace('\\', '/').TrimStart('/')),
                        kv.Value.Substring(folder.Length).Replace('\\', '/').TrimStart('/').Replace("%2B", "-"));
                }

                filter.ApplyInclude("_ProtobuildExternalProject\\.xml");
                filter.ApplyRewrite("_ProtobuildExternalProject\\.xml", "Build/Projects/" + packageName + ".definition");
                filter.ApplyInclude("_ProtobuildModule\\.xml");
                filter.ApplyRewrite("_ProtobuildModule\\.xml", "Build/Module.xml");

                filter.ImplyDirectories();

                _packageCreator.Create(
                    target,
                    filter,
                    folder,
                    format);

                Console.WriteLine("Package conversion complete.");
                bytes = new byte[target.Position];
                target.Seek(0, SeekOrigin.Begin);
                target.Read(bytes, 0, bytes.Length);
            }
            finally
            {
                Console.WriteLine("Cleaning up temporary data...");
                PathUtils.AggressiveDirectoryDelete(folder);
            }

            return bytes;
        }

        private string DownloadOrUseExistingNuGetPackage(string repoUrl, string packageName, string gitReference)
        {
            var nugetPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget",
                "packages",
                packageName,
                gitReference);
            if (Directory.Exists(nugetPath))
            {
                return nugetPath;
            }

            var packagesUrl = repoUrl + "/Packages(Id='" + packageName + "',Version='" + gitReference + "')";
            var client = new RetryableWebClient();
            
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
            try
            {
                using (var writer = new FileStream(tempFile, FileMode.Truncate, FileAccess.Write))
                {
                    writer.Write(downloadedZipData, 0, downloadedZipData.Length);
                }

                Directory.CreateDirectory(nugetPath);

                Console.WriteLine("Extracting package to " + nugetPath + "...");

                using (var zip = ZipStorer.Open(tempFile, FileAccess.Read))
                {
                    var files = zip.ReadCentralDir();

                    foreach (var file in files)
                    {
                        var targetPath = Path.Combine(nugetPath, file.FilenameInZip);
                        var directory = new FileInfo(targetPath).DirectoryName;
                        if (directory != null) Directory.CreateDirectory(directory);
                        zip.ExtractFile(file, targetPath);
                    }
                }

                Console.WriteLine("Extraction complete.");
            }
            finally
            {
                File.Delete(tempFile);
            }

            return nugetPath;
        }
    }
}

