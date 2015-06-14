using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Protobuild
{
    public class PackageCreator : IPackageCreator
    {
        private readonly IDeduplicator _deduplicator;
        
        public PackageCreator(IDeduplicator deduplicator)
        {
            _deduplicator = deduplicator;
        }

        public void Create(Stream target, FileFilter filter, string basePath, string packageFormat)
        {
            var archive = new MemoryStream();

            switch (packageFormat)
            {
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                    {
                        Console.WriteLine("Writing package in tar/gzip format...");
                        break;
                    }
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                default:
                    {
                        Console.WriteLine("Writing package in tar/lzma format...");
                        break;
                    }
            }

            switch (packageFormat)
            {
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                default:
                    {
                        var state = _deduplicator.CreateState();

                        Console.Write("Deduplicating files in package...");

                        var progressHelper = new DedupProgressRenderer(filter.Count());
                        var current = 0;

                        foreach (var kv in filter.OrderBy(kv => kv.Value))
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

                        Console.WriteLine();
                        Console.WriteLine("Adding files to package...");

                        using (var writer = new tar_cs.TarWriter(archive))
                        {
                            _deduplicator.PushToTar(state, writer);
                        }

                        break;
                    }
            }

            archive.Seek(0, SeekOrigin.Begin);

            switch (packageFormat)
            {
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

                        Console.WriteLine();

                        break;
                    }
                }
        }
    }
}

