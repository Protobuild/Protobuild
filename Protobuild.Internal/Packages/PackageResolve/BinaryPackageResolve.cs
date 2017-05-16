using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Protobuild
{
    internal class BinaryPackageResolve : IPackageResolve
    {
        private readonly SourcePackageResolve _sourcePackageResolve;
        private readonly IProjectTemplateApplier _projectTemplateApplier;
        private readonly IPackageGlobalTool _packageGlobalTool;
        private readonly IPackageCacheConfiguration _packageCacheConfiguration;
        private readonly IProgressiveWebOperation _progressiveWebOperation;
        private readonly INuGetPlatformMapping _nugetPlatformMapping;
        private readonly LightweightKernel _lightweightKernel;
        private IKnownToolProvider _knownToolProvider;

        public BinaryPackageResolve(
            SourcePackageResolve sourcePackageResolve, 
            IProjectTemplateApplier projectTemplateApplier, 
            IPackageGlobalTool packageGlobalTool,
            IPackageCacheConfiguration packageCacheConfiguration, 
            IProgressiveWebOperation progressiveWebOperation,
            INuGetPlatformMapping nugetPlatformMapping,
            LightweightKernel lightweightKernel)
        {
            _sourcePackageResolve = sourcePackageResolve;
            _projectTemplateApplier = projectTemplateApplier;
            _packageGlobalTool = packageGlobalTool;
            _packageCacheConfiguration = packageCacheConfiguration;
            _progressiveWebOperation = progressiveWebOperation;
            _nugetPlatformMapping = nugetPlatformMapping;
            _lightweightKernel = lightweightKernel;
        }

        public void Resolve(string workingDirectory, IPackageMetadata metadata, string folder, string templateName, bool forceUpgrade)
        {
            var protobuildMetadata = metadata as ProtobuildPackageMetadata;
            var nuGet3Metadata = metadata as NuGet3PackageMetadata;
            var transformedMetadata = metadata as TransformedPackageMetadata;

            if (protobuildMetadata != null)
            {
                ResolveProtobuild(workingDirectory, protobuildMetadata, folder, templateName, forceUpgrade);
                return;
            }

            if (nuGet3Metadata != null)
            {
                ResolveNuGet3(workingDirectory, nuGet3Metadata, folder, templateName, forceUpgrade);
                return;
            }

            if (transformedMetadata != null)
            {
                ResolveTransformed(workingDirectory, transformedMetadata, folder, templateName, forceUpgrade);
                return;
            }

            throw new InvalidOperationException("Unexpected metadata type " + metadata.GetType().Name + " for binary resolve.");
        }

        private void ResolveProtobuild(string workingDirectory, ProtobuildPackageMetadata protobuildMetadata, string folder, string templateName, bool forceUpgrade)
        {
            switch (protobuildMetadata.PackageType)
            {
                case PackageManager.PACKAGE_TYPE_LIBRARY:
                    ResolveLibraryBinary(workingDirectory, protobuildMetadata, Path.Combine(workingDirectory, folder), forceUpgrade, () =>
                    {
                        var package = GetBinaryPackage(protobuildMetadata);
                        if (package == null)
                        {
                            _sourcePackageResolve.Resolve(workingDirectory, protobuildMetadata, folder, null, forceUpgrade);
                            return null;
                        }
                        return package;
                    });
                    break;
                case PackageManager.PACKAGE_TYPE_TEMPLATE:
                    ResolveTemplateBinary(workingDirectory, protobuildMetadata, folder, templateName, forceUpgrade);
                    break;
                case PackageManager.PACKAGE_TYPE_GLOBAL_TOOL:
                    ResolveGlobalToolBinary(workingDirectory, protobuildMetadata, forceUpgrade);
                    break;
                default:
                    throw new InvalidOperationException("Unable to resolve binary package with type '" + protobuildMetadata.PackageType + "' using Protobuild-based package.");
            }
        }

        private void ResolveNuGet3(string workingDirectory, NuGet3PackageMetadata nuGet3Metadata, string folder, string templateName, bool forceUpgrade)
        {
            switch (nuGet3Metadata.PackageType)
            {
                case PackageManager.PACKAGE_TYPE_LIBRARY:
                    ResolveLibraryBinary(workingDirectory, nuGet3Metadata, Path.Combine(workingDirectory, folder), forceUpgrade, () =>
                    {
                        var package = GetBinaryPackage(nuGet3Metadata);
                        if (package == null)
                        {
                            _sourcePackageResolve.Resolve(workingDirectory, nuGet3Metadata, folder, null, forceUpgrade);
                            return null;
                        }
                        return package;
                    });
                    break;
                case PackageManager.PACKAGE_TYPE_TEMPLATE:
                    ResolveTemplateBinary(workingDirectory, nuGet3Metadata, folder, templateName, forceUpgrade);
                    break;
                case PackageManager.PACKAGE_TYPE_GLOBAL_TOOL:
                    ResolveGlobalToolBinary(workingDirectory, nuGet3Metadata, forceUpgrade);
                    break;
                default:
                    throw new InvalidOperationException("Unable to resolve binary package with type '" + nuGet3Metadata.PackageType + "' using Protobuild-based package.");
            }
        }

        private void ResolveTransformed(string workingDirectory, TransformedPackageMetadata transformedMetadata, string folder, string templateName, bool forceUpgrade)
        {
            switch (transformedMetadata.PackageType)
            {
                case PackageManager.PACKAGE_TYPE_LIBRARY:
                    ResolveLibraryBinary(workingDirectory, transformedMetadata, Path.Combine(workingDirectory, folder), forceUpgrade, () =>
                    {
                        var package = GetTransformedBinaryPackage(workingDirectory, transformedMetadata);
                        if (package == null)
                        {
                            throw new InvalidOperationException("Unable to transform " + transformedMetadata.SourceURI + " for usage as a Protobuild package.");
                        }
                        return package;
                    });
                    break;
                default:
                    throw new InvalidOperationException("Unable to resolve binary package with type '" + transformedMetadata.PackageType + "' using transformer-based package.");
            }
        }
        
        private void ResolveLibraryBinary(string workingDirectory, ICachableBinaryPackageMetadata protobuildMetadata, string folder, bool forceUpgrade, Func<byte[]> getBinaryPackage)
        {
            var platformFolder = Path.Combine(folder, protobuildMetadata.Platform);

            if (File.Exists(Path.Combine(platformFolder, ".pkg")))
            {
                if (!forceUpgrade)
                {
                    RedirectableConsole.WriteLine("Protobuild binary package already present at " + platformFolder);
                    return;
                }
            }

            RedirectableConsole.WriteLine("Creating and emptying " + platformFolder);

            if (File.Exists(Path.Combine(folder, ".pkg")))
            {
                if (Directory.Exists(platformFolder))
                {
                    // Only clear out the target's folder if the reference folder
                    // already contains binary packages (for other platforms)
                    PathUtils.AggressiveDirectoryDelete(platformFolder);
                }
            }
            else
            {
                // The reference folder is holding source code, so clear it
                // out entirely.
                PathUtils.AggressiveDirectoryDelete(folder);
            }

            Directory.CreateDirectory(platformFolder);

            RedirectableConsole.WriteLine("Marking " + folder + " as ignored for Git");
            GitUtils.MarkIgnored(folder);

            var package = getBinaryPackage();
            if (package == null)
            {
                return;
            }

            ExtractTo(workingDirectory, protobuildMetadata.PackageName, protobuildMetadata.BinaryFormat, package, platformFolder, protobuildMetadata.Platform);

            // Only copy ourselves to the binary folder if both "Build/Module.xml" and
            // "Build/Projects" exist in the binary package's folder.  This prevents us
            // from triggering the "create new module?" logic if the package hasn't been
            // setup correctly.
            if (Directory.Exists(Path.Combine(platformFolder, "Build", "Projects")) &&
                File.Exists(Path.Combine(platformFolder, "Build", "Module.xml")))
            {
                var sourceProtobuild = Assembly.GetEntryAssembly().Location;
                File.Copy(sourceProtobuild, Path.Combine(platformFolder, "Protobuild.exe"), true);

                try
                {
                    var chmodStartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = "a+x Protobuild.exe",
                        WorkingDirectory = platformFolder,
                        UseShellExecute = false
                    };
                    Process.Start(chmodStartInfo);
                }
                catch (ExecEnvironment.SelfInvokeExitException)
                {
                    throw;
                }
                catch
                {
                }
            }

            var file = File.Create(Path.Combine(platformFolder, ".pkg"));
            file.Close();

            file = File.Create(Path.Combine(folder, ".pkg"));
            file.Close();

            RedirectableConsole.WriteLine("Binary resolution complete");
        }

        private void ResolveTemplateBinary(string workingDirectory, ICachableBinaryPackageMetadata protobuildMetadata, string folder, string templateName, bool forceUpgrade)
        {
            if (folder != string.Empty)
            {
                throw new InvalidOperationException("Reference folder must be empty for template type.");
            }

            // The template is a reference to a Git repository.
            if (Directory.Exists(Path.Combine(workingDirectory, ".staging")))
            {
                PathUtils.AggressiveDirectoryDelete(Path.Combine(workingDirectory, ".staging"));
            }

            Directory.CreateDirectory(Path.Combine(workingDirectory, ".staging"));

            var package = GetBinaryPackage(protobuildMetadata);
            if (package == null)
            {
                _sourcePackageResolve.Resolve(workingDirectory, protobuildMetadata, folder, templateName, forceUpgrade);
                return;
            }

            ExtractTo(workingDirectory, protobuildMetadata.PackageName, protobuildMetadata.BinaryFormat, package, Path.Combine(workingDirectory, ".staging"), "Template");

            _projectTemplateApplier.Apply(Path.Combine(workingDirectory, ".staging"), templateName);
            PathUtils.AggressiveDirectoryDelete(Path.Combine(workingDirectory, ".staging"));
        }

        private void ResolveGlobalToolBinary(string workingDirectory, ICachableBinaryPackageMetadata protobuildMetadata, bool forceUpgrade)
        {
            var toolFolder = _packageGlobalTool.GetGlobalToolInstallationPath(protobuildMetadata.CanonicalURI);

            if (File.Exists(Path.Combine(toolFolder, ".pkg")))
            {
                if (!forceUpgrade)
                {
                    RedirectableConsole.WriteLine("Protobuild binary package already present at " + toolFolder);
                    return;
                }
            }

            RedirectableConsole.WriteLine("Creating and emptying " + toolFolder);
            PathUtils.AggressiveDirectoryDelete(toolFolder);
            Directory.CreateDirectory(toolFolder);

            RedirectableConsole.WriteLine("Installing " + protobuildMetadata.CanonicalURI + " at version " + protobuildMetadata.GitCommitOrRef);
            var package = GetBinaryPackage(protobuildMetadata);
            if (package == null)
            {
                RedirectableConsole.WriteLine("The specified global tool package is not available for this platform.");
                return;
            }

            ExtractTo(workingDirectory, protobuildMetadata.PackageName, protobuildMetadata.BinaryFormat, package, toolFolder, protobuildMetadata.Platform);

            var file = File.Create(Path.Combine(toolFolder, ".pkg"));
            file.Close();

            if (_knownToolProvider == null)
            {
                // We must delay load this because of a circular dependency :(
                _knownToolProvider = _lightweightKernel.Get<IKnownToolProvider>();
            }

            _packageGlobalTool.ScanPackageForToolsAndInstall(toolFolder, _knownToolProvider);

            RedirectableConsole.WriteLine("Binary resolution complete");
        }

        private byte[] GetBinaryPackage(ICachableBinaryPackageMetadata metadata)
        {
            if (metadata.BinaryFormat == null || metadata.BinaryUri == null)
            {
                // There is no binary format for this package.
                return null;
            }

            var localFileExists = false;
            try
            {
                localFileExists = File.Exists(metadata.BinaryUri);
            }
            catch
            {
            }

            if (metadata.BinaryFormat != null && localFileExists)
            {
                // This is a local package file, read it directly.
                using (var stream = new FileStream(metadata.BinaryUri, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return data;
                }
            }

            if (this.HasBinaryPackage(metadata))
            {
                // We have it already downloaded in the cache.
                var file = Path.Combine(
                    _packageCacheConfiguration.GetCacheDirectory(),
                    this.GetPackageName(metadata));
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return data;
                }
            }

            // We must use the package retrieval interface to download a copy
            // of the package.
            var tempFile = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(metadata) + ".tmp");
            if (!DownloadBinaryPackage(metadata, tempFile))
            {
                return null;
            }

            var saveFile = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(metadata));
            try
            {
                File.Move(tempFile, saveFile);
            }
            catch (Exception)
            {
                RedirectableConsole.WriteLine("WARNING: Unable to save package to cache.");
                saveFile = tempFile;
            }

            byte[] saveData;
            using (var stream = new FileStream(saveFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                saveData = new byte[stream.Length];
                stream.Read(saveData, 0, saveData.Length);
            }

            if (saveFile == tempFile)
            {
                File.Delete(tempFile);
            }

            return saveData;
        }

        private byte[] GetTransformedBinaryPackage(string workingDirectory, TransformedPackageMetadata metadata)
        {
            if (this.HasBinaryPackage(metadata))
            {
                // We have it already downloaded in the cache.
                var file = Path.Combine(
                    _packageCacheConfiguration.GetCacheDirectory(),
                    this.GetPackageName(metadata));
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return data;
                }
            }

            var packageData = metadata.Transformer.Transform(
                workingDirectory,
                metadata.SourceURI,
                metadata.GitRef,
                metadata.Platform,
                PackageManager.ARCHIVE_FORMAT_TAR_LZMA);
            if (packageData == null)
            {
                throw new InvalidOperationException("Unable to transform " + metadata.SourceURI + " for usage as a Protobuild package.");
            }

            var saveFile = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(metadata));

            var attempts = 10;
            while (attempts > 0)
            {
                try
                {
                    using (var stream = new FileStream(saveFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        stream.Write(packageData, 0, packageData.Length);
                    }
                    break;
                }
                catch (IOException)
                {
                    // On Windows, we can't write out the package file if another instance of Protobuild
                    // is writing it out at the moment.  Just wait and retry in another second.
                    RedirectableConsole.WriteLine("WARNING: Unable to write downloaded package file (attempt " + (11 - attempts) + " / 10)");
                    System.Threading.Thread.Sleep(5000);
                    attempts--;
                }
            }
            
            return packageData;
        }

        private bool HasBinaryPackage(ICachableBinaryPackageMetadata metadata)
        {
            var file = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(metadata));
            return File.Exists(file);
        }

        private string GetPackageName(ICachableBinaryPackageMetadata metadata)
        {
            var sha1 = new SHA1Managed();

            var urlHashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(NormalizeURIForCache(metadata.CanonicalURI)));
            var urlHashString = BitConverter.ToString(urlHashBytes).Replace("-", "").ToLowerInvariant();
            var gitHashString = metadata.GitCommitOrRef.ToLowerInvariant();
            var platformString = metadata.Platform.ToLowerInvariant();

            return urlHashString + "-" + gitHashString + "-" + platformString + TranslateToExtension(metadata.BinaryFormat);
        }

        private string NormalizeURIForCache(string canonicalUri)
        {
            var index = canonicalUri.IndexOf("://", StringComparison.InvariantCulture);

            if (index != -1)
            {
                return canonicalUri.Substring(index + "://".Length);
            }

            return canonicalUri;
        }

        private string TranslateToExtension(string format)
        {
            switch (format)
            {
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    return ".tar.lzma";
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                    return ".tar.gz";
                case PackageManager.ARCHIVE_FORMAT_NUGET_ZIP:
                    return ".nupkg";
                case "":
                    return string.Empty;
                default:
                    throw new InvalidOperationException("Archive format not supported in cache.");
            }
        }

        private bool DownloadBinaryPackage(ICachableBinaryPackageMetadata metadata, string targetPath)
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            var packageData = this.DownloadBinaryPackage(metadata);
            if (packageData == null)
            {
                // There is no binary package available.
                return false;
            }

            var attempts = 10;
            while (attempts > 0)
            {
                try
                {
                    using (var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        stream.Write(packageData, 0, packageData.Length);
                    }
                    break;
                }
                catch (IOException)
                {
                    // On Windows, we can't write out the package file if another instance of Protobuild
                    // is writing it out at the moment.  Just wait and retry in another second.
                    RedirectableConsole.WriteLine("WARNING: Unable to write downloaded package file (attempt " + (11 - attempts) + " / 10)");
                    System.Threading.Thread.Sleep(5000);
                    attempts--;
                }
            }

            if (attempts == 0)
            {
                RedirectableConsole.WriteLine(
                    "WARNING: Unable to write out downloaded package!  Assuming " +
                    "another instance of Protobuild will provide it.");
            }

            return true;
        }

        private byte[] DownloadBinaryPackage(ICachableBinaryPackageMetadata metadata)
        {
            try
            {
                return _progressiveWebOperation.Get(metadata.BinaryUri);
            }
            catch (InvalidOperationException)
            {
                RedirectableConsole.WriteLine("Unable to download binary package for version \"" + metadata.GitCommitOrRef + "\" and platform \"" + metadata.Platform + "\", falling back to source version");
                return null;
            }
        }

        private void ExtractTo(string workingDirectory, string packageName, string format, byte[] data, string path, string platform)
        {
            RedirectableConsole.WriteLine("Unpacking binary package from " + format + " archive");
            switch (format)
            {
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                    {
                        using (var memory = new MemoryStream(data))
                        {
                            using (var decompress = new GZipStream(memory, CompressionMode.Decompress))
                            {
                                using (var memory2 = new MemoryStream())
                                {
                                    decompress.CopyTo(memory2);
                                    memory2.Seek(0, SeekOrigin.Begin);
                                    var reader = new tar_cs.TarReader(memory2);
                                    var reduplicator = new Reduplicator();
                                    reduplicator.UnpackTarToFolder(reader, path);
                                }
                            }
                        }
                        break;
                    }
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    {
                        using (var inMemory = new MemoryStream(data))
                        {
                            using (var outMemory = new MemoryStream())
                            {
                                LZMA.LzmaHelper.Decompress(inMemory, outMemory);
                                outMemory.Seek(0, SeekOrigin.Begin);
                                var reader = new tar_cs.TarReader(outMemory);
                                var reduplicator = new Reduplicator();
                                reduplicator.UnpackTarToFolder(reader, path);
                            }
                        }
                        break;
                    }
                case PackageManager.ARCHIVE_FORMAT_NUGET_ZIP:
                    {
                        using (var inMemory = new MemoryStream(data))
                        {
                            using (var zipStorer = ZipStorer.Open(inMemory, FileAccess.Read, true))
                            {
                                var reduplicator = new Reduplicator();
                                var extractedFiles = reduplicator.UnpackZipToFolder(
                                    zipStorer, 
                                    path, 
                                    candidatePath => candidatePath.Replace('\\', '/').StartsWith("protobuild/" + platform + "/"),
                                    outputPath => outputPath.Replace('\\', '/').Substring(("protobuild/" + platform + "/").Length));

                                if (extractedFiles.Count == 0)
                                {
                                    // There were no files that matched protobuild/ which means this is
                                    // not a Protobuild-aware NuGet package.  We need to convert it on-the-fly
                                    // to a compatible Protobuild format.
                                    ConvertNuGetOnlyPackage(reduplicator, zipStorer, path, packageName, workingDirectory, platform);
                                }
                            }
                        }
                        break;
                    }
                default:
                    throw new InvalidOperationException(
                        "This version of Protobuild does not support the " +
                        format + " package format.");
            }
        }

        private void ConvertNuGetOnlyPackage(Reduplicator reduplicator, ZipStorer zipStorer, string path, string packageName, string workingDirectory, string platform)
        {
            var folder = Path.GetTempFileName();
            File.Delete(folder);
            Directory.CreateDirectory(folder);

            try
            {
                reduplicator.UnpackZipToFolder(
                    zipStorer,
                    folder,
                    candidatePath => true,
                    outputPath => outputPath);

                var references = new List<string>();
                var libraryReferences = new Dictionary<string, string>();
                var packageDependencies = new Dictionary<string, string>();

                // Load NuGet specification file.
                var specFile = Directory.GetFiles(folder, "*.nuspec").FirstOrDefault();
                if (specFile != null)
                {
                    specFile = Path.Combine(folder, specFile);
                    if (File.Exists(specFile))
                    {
                        var packageDoc = new XmlDocument();
                        packageDoc.Load(specFile);

                        if (packageDoc?.DocumentElement != null)
                        {
                            // If we have an id in the package, that forms the package name.
                            if (packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
                                    .Count(x => x.Name == "id") > 0)
                            {
                                var newName = packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
                                    .First(x => x.Name == "id").InnerText.Trim();
                                if (!string.IsNullOrWhiteSpace(newName))
                                {
                                    packageName = newName;
                                }
                            }

                            // If the references are explicitly provided in the nuspec, use
                            // those as to what files should be referenced by the projects.
                            if (packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
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
                            if (packageDoc.DocumentElement.FirstChild.ChildNodes.OfType<XmlElement>()
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
                                                RedirectableConsole.WriteLine("WARNING: More than one dependency on " +
                                                                              c +
                                                                              " in NuGet package."));
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(packageName))
                {
                    throw new InvalidOperationException("Expected package name when converting NuGet-only package!");
                }

                // Determine the priority of the frameworks that we want to target
                // out of the available versions.
                string[] clrNames = _nugetPlatformMapping.GetFrameworkNamesForRead(workingDirectory, platform);

                var referenceDirectories = new string[] { "ref", "lib" };

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
                    RedirectableConsole.WriteLine("Found library to reference: " + kv.Key + " (at " + kv.Value + ")");
                }
                
                RedirectableConsole.WriteLine("Generating external project reference...");
                var document = new XmlDocument();
                var externalProject = document.CreateElement("ExternalProject");
                externalProject.SetAttribute("Name", packageName);
                document.AppendChild(externalProject);
                foreach (var kv in libraryReferences)
                {
                    var binaryReference = document.CreateElement("Binary");
                    binaryReference.SetAttribute("Name", kv.Key);
                    binaryReference.SetAttribute("Path",
                        kv.Value.Substring(folder.Length).TrimStart(new[] { '/', '\\' }).Replace("%2B", "-"));
                    externalProject.AppendChild(binaryReference);
                }
                foreach (var package in packageDependencies)
                {
                    var externalReference = document.CreateElement("Reference");
                    externalReference.SetAttribute("Include", package.Key);
                    externalProject.AppendChild(externalReference);
                }
                Directory.CreateDirectory(Path.Combine(path, "Build", "Projects"));
                document.Save(Path.Combine(path, "Build", "Projects", packageName + ".definition"));

                RedirectableConsole.WriteLine("Generating module...");
                var generatedModule = new ModuleInfo();
                generatedModule.Name = packageName;
                generatedModule.Packages = new List<PackageRef>();

                foreach (var package in packageDependencies)
                {
                    generatedModule.Packages.Add(new PackageRef
                    {
                        Uri = "https-nuget-v3://api.nuget.org/v3/index.json|" + package.Key,
                        GitRef = package.Value.TrimStart('[').TrimEnd(']'),
                        Folder = package.Key
                    });
                }

                generatedModule.Save(Path.Combine(path, "Build", "Module.xml"));
                
                foreach (var kv in libraryReferences)
                {
                    var targetFile =
                        new FileInfo(Path.Combine(path,
                            kv.Value.Substring(folder.Length).Replace('\\', '/').TrimStart('/').Replace("%2B", "-")));
                    targetFile.Directory.Create();
                    File.Copy(kv.Value, targetFile.FullName);
                }
            }
            finally
            {
                PathUtils.AggressiveDirectoryDelete(folder);
            }
        }

        public void GetProtobuildPackageBinary(IPackageMetadata metadata, out string archiveType, out byte[] packageData)
        {
            var protobuildPackageMetadata = metadata as ProtobuildPackageMetadata;
            if (protobuildPackageMetadata == null)
            {
                throw new InvalidOperationException("Can't call GetProtobuildPackageBinary on non-Protobuild package metadata");
            }

            archiveType = protobuildPackageMetadata.BinaryFormat;
            packageData = GetBinaryPackage(protobuildPackageMetadata);
        }
    }
}