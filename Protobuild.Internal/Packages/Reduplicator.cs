using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Protobuild
{
    internal class Reduplicator
    {
        private Dictionary<string, byte[]> UnpackTar(tar_cs.TarReader reader, string folder, bool toMemory)
        {
            var results = new Dictionary<string, byte[]>();

            const string DedupPrefix = "_DedupFiles/";
            var hashesToStreams = new Dictionary<string, Stream>();

            while (reader.MoveNext(false))
            {
                if (reader.FileInfo.FileName.StartsWith(DedupPrefix) &&
                    reader.FileInfo.EntryType == tar_cs.EntryType.File)
                {
                    // This is a deduplicated archive; place the deduplicated
                    // files into the dictionary.
                    var hash = reader.FileInfo.FileName.Substring(DedupPrefix.Length);
                    var memory = new MemoryStream();
                    reader.Read(memory);
                    memory.Seek(0, SeekOrigin.Begin);
                    hashesToStreams.Add(hash, memory);
                    continue;
                }

                if (reader.FileInfo.FileName == DedupPrefix &&
                    reader.FileInfo.EntryType == tar_cs.EntryType.Directory)
                {
                    // This is the deduplication folder; ignore it.
                    continue;
                }

                var target = folder == null ? null : Path.Combine(folder, reader.FileInfo.FileName);

                try
                {
                    switch (reader.FileInfo.EntryType)
                    {
                        case tar_cs.EntryType.File:
                        case tar_cs.EntryType.FileObsolete:
                            if (toMemory)
                            {
                                using (var memory = new MemoryStream())
                                {
                                    reader.Read(memory);
                                    var data = new byte[(int)memory.Position];
                                    memory.Seek(0, SeekOrigin.Begin);
                                    memory.Read(data, 0, data.Length);
                                    results.Add(reader.FileInfo.FileName, data);
                                }
                            }
                            else
                            {
                                using (var writer = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    reader.Read(writer);
                                }
                            }
                            break;
                        case tar_cs.EntryType.HardLink:
                            if (reader.FileInfo.LinkName.StartsWith(DedupPrefix))
                            {
                                // This file has been deduplicated; resolve the actual file
                                // using the dictionary.
                                var hash = reader.FileInfo.LinkName.Substring(DedupPrefix.Length);
                                if (!hashesToStreams.ContainsKey(hash))
                                {
                                    Console.WriteLine("WARNING: Unable to find deduplicated stream for file hash " +
                                                      hash);
                                }
                                else
                                {
                                    if (toMemory)
                                    {
                                        using (var memory = new MemoryStream())
                                        {
                                            hashesToStreams[hash].CopyTo(memory);
                                            hashesToStreams[hash].Seek(0, SeekOrigin.Begin);
                                            var data = new byte[(int)memory.Position];
                                            memory.Seek(0, SeekOrigin.Begin);
                                            memory.Read(data, 0, data.Length);
                                            results.Add(reader.FileInfo.FileName, data);
                                        }
                                    }
                                    else
                                    {
                                        using (var writer = new FileStream(target, FileMode.Create, FileAccess.Write,
                                                FileShare.None))
                                        {
                                            hashesToStreams[hash].CopyTo(writer);
                                            hashesToStreams[hash].Seek(0, SeekOrigin.Begin);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("WARNING: Unknown hard link present in TAR archive.");
                            }
                            break;
                        case tar_cs.EntryType.Directory:
                            if (toMemory)
                            {
                                results.Add(reader.FileInfo.FileName.Trim('/') + "/", null);
                            }
                            else
                            {
                                Directory.CreateDirectory(target);
                            }
                            break;
                        default:
                            Console.WriteLine("WARNING: Ignoring unknown entry type in TAR archive.");
                            break;
                    }
                }
                catch (PathTooLongException ex)
                {
                    throw new PathTooLongException("The path '" + target + "' is too long to extract on this operating system.", ex);
                }
            }

            return results;
        }

        public void UnpackTarToFolder(tar_cs.TarReader reader, string folder)
        {
            UnpackTar(reader, folder, false);
        }

        public Dictionary<string, byte[]> UnpackTarToMemory(tar_cs.TarReader reader)
        {
            return UnpackTar(reader, null, true);
        }

        private Dictionary<string, byte[]> UnpackZip(ZipStorer zip, string folder, Func<string, bool> filterOutputPaths, Func<string, string> mutateOutputPaths, bool toMemory)
        {
            var results = new Dictionary<string, byte[]>();

            const string DedupPrefix = "_DedupFiles/";
            var hashesToStreams = new Dictionary<string, Stream>();

            var entries = zip.ReadCentralDir();
            
            if (entries.Any(x => x.FilenameInZip == "_DedupIndex.txt"))
            {
                // Extract and reduplicate the contents of the package.
                var indexFile = entries.First(x => x.FilenameInZip == "_DedupIndex.txt");
                using (var indexStream = new MemoryStream())
                {
                    zip.ExtractFile(indexFile, indexStream);
                    indexStream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(indexStream, Encoding.UTF8, true, 4096, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            var components = reader.ReadLine().Split(new[] { '?' }, 2);
                            if (components.Length != 2 || (components.Length >= 1 && string.IsNullOrWhiteSpace(components[0])))
                            {
                                Console.WriteLine("WARNING: Malformed index entry in deduplication index.");
                                continue;
                            }

                            var outputFilename = components[0];
                            var filenameInZip = components[1];

                            if (filterOutputPaths != null && !filterOutputPaths(outputFilename))
                            {
                                // Path was filtered out.
                                continue;
                            }

                            if (mutateOutputPaths != null)
                            {
                                outputFilename = mutateOutputPaths(outputFilename);
                            }

                            if (filenameInZip == "<DIRECTORY>")
                            {
                                // Explicitly create this directory.
                                var buildUpDir = string.Empty;
                                foreach (var dirComponent in outputFilename.Replace('\\', '/').Split(new[] { '/' }))
                                {
                                    var localDir = buildUpDir + dirComponent + "/";
                                    buildUpDir = localDir;

                                    if (toMemory)
                                    {
                                        if (!results.ContainsKey(localDir))
                                        {
                                            results.Add(localDir, null);
                                        }
                                    }
                                    else
                                    {
                                        Directory.CreateDirectory(folder.TrimEnd(new[] { '/', '\\' }) + '/' + NormalizeName(localDir));
                                    }
                                }
                                continue;
                            }

                            if (!entries.Any(x => x.FilenameInZip == filenameInZip))
                            {
                                Console.WriteLine("WARNING: Unable to locate deduplication data file in ZIP: " + filenameInZip);
                                continue;
                            }

                            var baseName = Path.GetDirectoryName(outputFilename);
                            var buildUp = string.Empty;
                            foreach (var dirComponent in baseName.Replace('\\', '/').Split(new[] { '/' }))
                            {
                                var localDir = buildUp + dirComponent + "/";
                                buildUp = localDir;

                                if (toMemory)
                                {
                                    if (!results.ContainsKey(localDir))
                                    {
                                        results.Add(localDir, null);
                                    }
                                }
                                else
                                {
                                    Directory.CreateDirectory(folder.TrimEnd(new[] { '/', '\\' }) + '/' + NormalizeName(localDir));
                                }
                            }

                            if (toMemory)
                            {
                                using (var stream = new MemoryStream())
                                {
                                    zip.ExtractFile(entries.First(x => x.FilenameInZip == filenameInZip), stream);
                                    var bytes = new byte[stream.Position];
                                    stream.Seek(0, SeekOrigin.Begin);
                                    stream.Read(bytes, 0, bytes.Length);
                                    results.Add(outputFilename, bytes);
                                }
                            }
                            else
                            {
                                zip.ExtractFile(entries.First(x => x.FilenameInZip == filenameInZip), folder.TrimEnd(new[] { '/', '\\' }) + '/' + NormalizeName(outputFilename));
                            }
                        }
                    }
                }
            }
            else
            {
                // Extract as-is.
                foreach (var entry in entries.OrderBy(x => x.FilenameInZip))
                {
                    if (filterOutputPaths != null && !filterOutputPaths(entry.FilenameInZip))
                    {
                        // Path was filtered out.
                        continue;
                    }

                    var baseName = Path.GetDirectoryName(entry.FilenameInZip);
                    var buildUp = string.Empty;
                    foreach (var dirComponent in baseName.Replace('\\', '/').Split(new[] { '/' }))
                    {
                        var localDir = buildUp + dirComponent + "/";
                        buildUp = localDir;

                        if (toMemory)
                        {
                            if (!results.ContainsKey(localDir))
                            {
                                results.Add(localDir, null);
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(folder.TrimEnd(new[] { '/', '\\' }) + '/' + NormalizeName(localDir));
                        }
                    }

                    if (toMemory)
                    {
                        using (var stream = new MemoryStream())
                        {
                            zip.ExtractFile(entry, stream);
                            var bytes = new byte[stream.Position];
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.Read(bytes, 0, bytes.Length);
                            results.Add(entry.FilenameInZip, bytes);
                        }
                    }
                    else
                    {
                        zip.ExtractFile(entry, folder.TrimEnd(new[] { '/', '\\' }) + '/' + NormalizeName(entry.FilenameInZip));
                    }
                }
            }

            return results;
        }

        private static string NormalizeName(string name)
        {
            name = name.Trim(new[] { '/', '\\' });
            name = name.Replace('/', Path.DirectorySeparatorChar);
            name = name.Replace('\\', Path.DirectorySeparatorChar);
            return name;
        }

        public void UnpackZipToFolder(ZipStorer zip, string folder, Func<string, bool> filterOutputPaths, Func<string, string> mutateOutputPaths)
        {
            UnpackZip(zip, folder, filterOutputPaths, mutateOutputPaths, false);
        }

        public Dictionary<string, byte[]> UnpackZipToMemory(ZipStorer zip, Func<string, bool> filterOutputPaths, Func<string, string> mutateOutputPaths)
        {
            return UnpackZip(zip, null, filterOutputPaths, mutateOutputPaths, true);
        }
    }
}

