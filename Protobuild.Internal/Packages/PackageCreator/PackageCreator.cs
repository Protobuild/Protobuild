using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Protobuild
{
    internal class PackageCreator : IPackageCreator
    {
        private readonly IDeduplicator _deduplicator;
        
        public PackageCreator(IDeduplicator deduplicator)
        {
            _deduplicator = deduplicator;
        }

        public void Create(Stream target, FileFilter filter, string basePath, string packageFormat, string platform)
        {
            Stream archive = new MemoryStream();
            string cleanUp = null;

            try
            {
                switch (packageFormat)
                {
                    case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                        Console.WriteLine("Writing package in tar/gzip format...");
                        break;
                    case PackageManager.ARCHIVE_FORMAT_NUGET_ZIP:
                        Console.WriteLine("Writing package in NuGet ZIP format... (this is experimental!)");
                        break;
                    case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    default:
                        Console.WriteLine("Writing package in tar/lzma format...");
                        break;
                }

                switch (packageFormat)
                {
                    case PackageManager.ARCHIVE_FORMAT_NUGET_ZIP:
                        {
                            if (platform == "Unified")
                            {
                                // With unified packages, the contents have already been deduplicated in the
                                // individual packages we're combining.  Therefore we don't deduplicate again
                                // because we already have the deduplication index calculated as part of the
                                // package contents.  Just add all the files to the ZIP instead.
                                Action<ZipStorer> addFilesToZip = zip =>
                                {
                                    Console.WriteLine("Adding files to package...");

                                    var progressHelper = new AddFilesProgressRenderer(filter.CountTargetPaths());
                                    var current = 0;

                                    var uniqueFiles = new HashSet<string>();

                                    var entries = filter.GetExpandedEntries();

                                    foreach (var kv in entries.OrderBy(kv => kv.Value))
                                    {
                                        if (kv.Value.EndsWith("/"))
                                        {
                                            // Directory - do nothing
                                        }
                                        else
                                        {
                                            var realFile = Path.Combine(basePath, kv.Key);

                                            if (uniqueFiles.Contains(kv.Value))
                                            {
                                                // Already exists - do nothing
                                            }
                                            else
                                            {
                                                zip.AddFile(
                                                    ZipStorer.Compression.Deflate,
                                                    realFile,
                                                    kv.Value,
                                                    string.Empty);
                                                uniqueFiles.Add(kv.Value);
                                            }
                                        }

                                        current++;

                                        progressHelper.SetProgress(current);
                                    }

                                    progressHelper.FinalizeRendering();
                                };
                                
                                try
                                {
                                    using (var zip = ZipStorer.Create(target, string.Empty, true))
                                    {
                                        addFilesToZip(zip);
                                    }
                                }
                                catch (OutOfMemoryException)
                                {
                                    // It's possible the archive is too large to store in memory.  Fall
                                    // back to using a temporary file on disk.
                                    cleanUp = Path.GetTempFileName();
                                    Console.WriteLine(
                                        "WARNING: Out-of-memory while creating ZIP file, falling back to storing " +
                                        "temporary file on disk at " + cleanUp + " during package creation.");
                                    archive.Dispose();
                                    archive = new FileStream(cleanUp, FileMode.Create, FileAccess.ReadWrite);

                                    using (var zip = ZipStorer.Create(archive, string.Empty, true))
                                    {
                                        addFilesToZip(zip);
                                    }
                                }
                            }
                            else
                            {
                                // Deduplicate the contents of the ZIP package.
                                var state = _deduplicator.CreateState();

                                Console.Write("Deduplicating files in package...");

                                var progressHelper = new DedupProgressRenderer(filter.CountTargetPaths());
                                var current = 0;

                                var entries = filter.GetExpandedEntries();

                                foreach (var kv in entries.OrderBy(kv => kv.Value))
                                {
                                    if (kv.Value.EndsWith("/"))
                                    {
                                        // Directory
                                        _deduplicator.AddDirectory(state, kv.Value);
                                    }
                                    else
                                    {
                                        // File
                                        var realFile = Path.Combine(basePath, kv.Key);
                                        var realFileInfo = new FileInfo(realFile);

                                        _deduplicator.AddFile(state, realFileInfo, kv.Value);
                                    }

                                    current++;

                                    progressHelper.SetProgress(current);
                                }

                                progressHelper.FinalizeRendering();
                                Console.WriteLine("Adding files to package...");

                                try
                                {
                                    using (var zip = ZipStorer.Create(target, string.Empty, true))
                                    {
                                        _deduplicator.PushToZip(state, zip);
                                    }
                                }
                                catch (OutOfMemoryException)
                                {
                                    // It's possible the archive is too large to store in memory.  Fall
                                    // back to using a temporary file on disk.
                                    cleanUp = Path.GetTempFileName();
                                    Console.WriteLine(
                                        "WARNING: Out-of-memory while creating ZIP file, falling back to storing " +
                                        "temporary file on disk at " + cleanUp + " during package creation.");
                                    archive.Dispose();
                                    archive = new FileStream(cleanUp, FileMode.Create, FileAccess.ReadWrite);

                                    using (var zip = ZipStorer.Create(archive, string.Empty, true))
                                    {
                                        _deduplicator.PushToZip(state, zip);
                                    }
                                }
                            }

                            break;
                        }
                    case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                    case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    default:
                        {
                            var state = _deduplicator.CreateState();

                            Console.Write("Deduplicating files in package...");

                            var progressHelper = new DedupProgressRenderer(filter.CountTargetPaths());
                            var current = 0;

                            var entries = filter.GetExpandedEntries();

                            foreach (var kv in entries.OrderBy(kv => kv.Value))
                            {
                                if (kv.Value.EndsWith("/"))
                                {
                                    // Directory
                                    _deduplicator.AddDirectory(state, kv.Value);
                                }
                                else
                                {
                                    // File
                                    var realFile = Path.Combine(basePath, kv.Key);
                                    var realFileInfo = new FileInfo(realFile);

                                    _deduplicator.AddFile(state, realFileInfo, kv.Value);
                                }

                                current++;

                                progressHelper.SetProgress(current);
                            }

                            progressHelper.FinalizeRendering();
                            Console.WriteLine("Adding files to package...");

                            try
                            {
                                using (var writer = new tar_cs.TarWriter(archive))
                                {
                                    _deduplicator.PushToTar(state, writer);
                                }
                            }
                            catch (OutOfMemoryException)
                            {
                                // It's possible the archive is too large to store in memory.  Fall
                                // back to using a temporary file on disk.
                                cleanUp = Path.GetTempFileName();
                                Console.WriteLine(
                                    "WARNING: Out-of-memory while creating TAR file, falling back to storing " +
                                    "temporary file on disk at " + cleanUp + " during package creation.");
                                archive.Dispose();
                                archive = new FileStream(cleanUp, FileMode.Create, FileAccess.ReadWrite);
                                
                                using (var writer = new tar_cs.TarWriter(archive))
                                {
                                    _deduplicator.PushToTar(state, writer);
                                }
                            }

                            break;
                        }
                }

                archive.Seek(0, SeekOrigin.Begin);

                switch (packageFormat)
                {
                    case PackageManager.ARCHIVE_FORMAT_NUGET_ZIP:
                        // No need to compress anything further.
                        break;
                    case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                        {
                            Console.WriteLine("Compressing package...");

                            using (var compress = new GZipStream(target, CompressionMode.Compress))
                            {
                                archive.CopyTo(compress);
                            }

                            break;
                        }
                    case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    default:
                        {
                            Console.Write("Compressing package...");

                            var progressHelper = new CompressProgressRenderer(archive.Length);

                            LZMA.LzmaHelper.Compress(archive, target, progressHelper);

                            progressHelper.FinalizeRendering();

                            break;
                        }
                }
            }
            finally
            {
                if (cleanUp != null)
                {
                    try
                    {
                        File.Delete(cleanUp);
                    }
                    catch
                    {
                        Console.WriteLine("WARNING: Unable to clean up temporary package file at " + cleanUp);
                    }
                }
            }
        }
    }
}

