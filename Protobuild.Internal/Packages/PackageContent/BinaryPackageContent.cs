using System;
using System.IO;
using System.IO.Compression;

namespace Protobuild
{
    public class BinaryPackageContent : IPackageContent
    {
        public string Format { get; set; }

        public byte[] PackageData { get; set; }

        public void ExtractTo(string path)
        {
            Console.WriteLine("Unpacking binary package from " + this.Format + " archive");
            switch (this.Format)
            {
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                    {
                        using (var memory = new MemoryStream(this.PackageData))
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
                        using (var inMemory = new MemoryStream(this.PackageData))
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
                        this.Format + " package format.");
            }
        }
    }
}

