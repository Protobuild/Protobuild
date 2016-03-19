using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Protobuild
{
    internal class BinaryPackageResolve : IPackageResolve
    {
        private readonly SourcePackageResolve _sourcePackageResolve;
        private readonly IProjectTemplateApplier _projectTemplateApplier;
        private readonly IPackageGlobalTool _packageGlobalTool;
        private readonly IPackageCacheConfiguration _packageCacheConfiguration;
        private readonly IProgressiveWebOperation _progressiveWebOperation;

        public BinaryPackageResolve(
            SourcePackageResolve sourcePackageResolve, 
            IProjectTemplateApplier projectTemplateApplier, 
            IPackageGlobalTool packageGlobalTool,
            IPackageCacheConfiguration packageCacheConfiguration, 
            IProgressiveWebOperation progressiveWebOperation)
        {
            _sourcePackageResolve = sourcePackageResolve;
            _projectTemplateApplier = projectTemplateApplier;
            _packageGlobalTool = packageGlobalTool;
            _packageCacheConfiguration = packageCacheConfiguration;
            _progressiveWebOperation = progressiveWebOperation;
        }

        public void Resolve(IPackageMetadata metadata, string folder, string templateName, bool forceUpgrade)
        {
            var protobuildMetadata = metadata as ProtobuildPackageMetadata;
            var transformedMetadata = metadata as TransformedPackageMetadata;

            if (protobuildMetadata != null)
            {
                ResolveProtobuild(protobuildMetadata, folder, templateName, forceUpgrade);
                return;
            }

            if (transformedMetadata != null)
            {
                ResolveTransformed(transformedMetadata, folder, templateName, forceUpgrade);
                return;
            }

            throw new InvalidOperationException("Unexpected metadata type " + metadata.GetType().Name + " for binary resolve.");
        }

        private void ResolveProtobuild(ProtobuildPackageMetadata protobuildMetadata, string folder, string templateName, bool forceUpgrade)
        {
            switch (protobuildMetadata.PackageType)
            {
                case PackageManager.PACKAGE_TYPE_LIBRARY:
                    ResolveLibraryBinary(protobuildMetadata, folder, forceUpgrade, () =>
                    {
                        var package = GetProtobuildBinaryPackage(protobuildMetadata);
                        if (package == null)
                        {
                            _sourcePackageResolve.Resolve(protobuildMetadata, folder, null, forceUpgrade);
                            return null;
                        }
                        return package;
                    });
                    break;
                case PackageManager.PACKAGE_TYPE_TEMPLATE:
                    ResolveTemplateBinary(protobuildMetadata, folder, templateName, forceUpgrade);
                    break;
                case PackageManager.PACKAGE_TYPE_GLOBAL_TOOL:
                    ResolveGlobalToolBinary(protobuildMetadata, forceUpgrade);
                    break;
                default:
                    throw new InvalidOperationException("Unable to resolve binary package with type '" + protobuildMetadata.PackageType + "' using Protobuild-based package.");
            }
        }

        private void ResolveTransformed(TransformedPackageMetadata transformedMetadata, string folder, string templateName, bool forceUpgrade)
        {
            switch (transformedMetadata.PackageType)
            {
                case PackageManager.PACKAGE_TYPE_LIBRARY:
                    ResolveLibraryBinary(transformedMetadata, folder, forceUpgrade, () =>
                    {
                        var package = GetTransformedBinaryPackage(transformedMetadata);
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
        
        private void ResolveLibraryBinary(ICachableBinaryPackageMetadata protobuildMetadata, string folder, bool forceUpgrade, Func<byte[]> getBinaryPackage)
        {
            var platformFolder = Path.Combine(folder, protobuildMetadata.Platform);

            if (File.Exists(Path.Combine(platformFolder, ".pkg")))
            {
                if (!forceUpgrade)
                {
                    Console.WriteLine("Protobuild binary package already present at " + platformFolder);
                    return;
                }
            }

            Console.WriteLine("Creating and emptying " + platformFolder);

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

            Console.WriteLine("Marking " + folder + " as ignored for Git");
            GitUtils.MarkIgnored(folder);

            var package = getBinaryPackage();
            if (package == null)
            {
                return;
            }

            ExtractTo(protobuildMetadata.BinaryFormat, package, platformFolder);

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

            Console.WriteLine("Binary resolution complete");
        }

        private void ResolveTemplateBinary(ProtobuildPackageMetadata protobuildMetadata, string folder, string templateName, bool forceUpgrade)
        {
            if (folder != string.Empty)
            {
                throw new InvalidOperationException("Reference folder must be empty for template type.");
            }

            // The template is a reference to a Git repository.
            if (Directory.Exists(".staging"))
            {
                PathUtils.AggressiveDirectoryDelete(".staging");
            }

            Directory.CreateDirectory(".staging");

            var package = GetProtobuildBinaryPackage(protobuildMetadata);
            if (package == null)
            {
                _sourcePackageResolve.Resolve(protobuildMetadata, folder, templateName, forceUpgrade);
                return;
            }

            ExtractTo(protobuildMetadata.BinaryFormat, package, ".staging");

            _projectTemplateApplier.Apply(".staging", templateName);
            PathUtils.AggressiveDirectoryDelete(".staging");
        }

        private void ResolveGlobalToolBinary(ProtobuildPackageMetadata protobuildMetadata, bool forceUpgrade)
        {
            var toolFolder = _packageGlobalTool.GetGlobalToolInstallationPath(protobuildMetadata.ReferenceURI);

            if (File.Exists(Path.Combine(toolFolder, ".pkg")))
            {
                if (!forceUpgrade)
                {
                    Console.WriteLine("Protobuild binary package already present at " + toolFolder);
                    return;
                }
            }

            Console.WriteLine("Creating and emptying " + toolFolder);
            PathUtils.AggressiveDirectoryDelete(toolFolder);
            Directory.CreateDirectory(toolFolder);

            Console.WriteLine("Installing " + protobuildMetadata.ReferenceURI + " at version " + protobuildMetadata.GitCommit);
            var package = GetProtobuildBinaryPackage(protobuildMetadata);
            if (package == null)
            {
                Console.WriteLine("The specified global tool package is not available for this platform.");
                return;
            }

            ExtractTo(protobuildMetadata.BinaryFormat, package, toolFolder);

            var file = File.Create(Path.Combine(toolFolder, ".pkg"));
            file.Close();

            _packageGlobalTool.ScanPackageForToolsAndInstall(toolFolder);

            Console.WriteLine("Binary resolution complete");
        }

        private byte[] GetProtobuildBinaryPackage(ProtobuildPackageMetadata metadata)
        {
            if (metadata.BinaryFormat == null || metadata.BinaryURI == null)
            {
                // There is no binary format for this package.
                return null;
            }

            var localFileExists = false;
            try
            {
                localFileExists = File.Exists(metadata.BinaryURI);
            }
            catch
            {
            }

            if (metadata.BinaryFormat != null && localFileExists)
            {
                // This is a local package file, read it directly.
                using (var stream = new FileStream(metadata.BinaryURI, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                Console.WriteLine("WARNING: Unable to save package to cache.");
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

        private byte[] GetTransformedBinaryPackage(TransformedPackageMetadata metadata)
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
                    Console.WriteLine("WARNING: Unable to write downloaded package file (attempt " + (11 - attempts) + " / 10)");
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
                case "":
                    return string.Empty;
                default:
                    throw new InvalidOperationException("Archive format not supported in cache.");
            }
        }

        private bool DownloadBinaryPackage(ProtobuildPackageMetadata metadata, string targetPath)
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
                    Console.WriteLine("WARNING: Unable to write downloaded package file (attempt " + (11 - attempts) + " / 10)");
                    System.Threading.Thread.Sleep(5000);
                    attempts--;
                }
            }

            if (attempts == 0)
            {
                Console.WriteLine(
                    "WARNING: Unable to write out downloaded package!  Assuming " +
                    "another instance of Protobuild will provide it.");
            }

            return true;
        }

        private byte[] DownloadBinaryPackage(ProtobuildPackageMetadata metadata)
        {
            try
            {
                return _progressiveWebOperation.Get(metadata.BinaryURI);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Unable to download binary package for version \"" + metadata.GitCommit + "\" and platform \"" + metadata.Platform + "\", falling back to source version");
                return null;
            }
        }

        private void ExtractTo(string format, byte[] data, string path)
        {
            Console.WriteLine("Unpacking binary package from " + format + " archive");
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
                default:
                    throw new InvalidOperationException(
                        "This version of Protobuild does not support the " +
                        format + " package format.");
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
            packageData = GetProtobuildBinaryPackage(protobuildPackageMetadata);
        }
    }
}