using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Protobuild
{
    public class Reduplicator
    {
        public void UnpackTarToFolder(tar_cs.TarReader reader, string folder)
        {
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

                switch (reader.FileInfo.EntryType)
                {
                    case tar_cs.EntryType.File:
                    case tar_cs.EntryType.FileObsolete:
                        using (var writer = new FileStream(Path.Combine(folder, reader.FileInfo.FileName), FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            reader.Read(writer);
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
                                Console.WriteLine("WARNING: Unable to find deduplicated stream for file hash " + hash);
                            }
                            else
                            {
                                using (var writer = new FileStream(Path.Combine(folder, reader.FileInfo.FileName), FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    hashesToStreams[hash].CopyTo(writer);
                                    hashesToStreams[hash].Seek(0, SeekOrigin.Begin);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Unknown hard link present in TAR archive.");
                        }
                        break;
                    case tar_cs.EntryType.Directory:
                        Directory.CreateDirectory(Path.Combine(folder, reader.FileInfo.FileName));
                        break;
                    default:
                        Console.WriteLine("WARNING: Ignoring unknown entry type in TAR archive.");
                        break;
                }
            }
        }

        public Dictionary<string, byte[]> UnpackTarToMemory(tar_cs.TarReader reader)
        {
            const string DedupPrefix = "_DedupFiles/";
            var hashesToStreams = new Dictionary<string, Stream>();
            var results = new Dictionary<string, byte[]>();

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

                switch (reader.FileInfo.EntryType)
                {
                    case tar_cs.EntryType.File:
                    case tar_cs.EntryType.FileObsolete:
                        using (var memory = new MemoryStream())
                        {
                            reader.Read(memory);
                            var data = new byte[(int)memory.Position];
                            memory.Seek(0, SeekOrigin.Begin);
                            memory.Read(data, 0, data.Length);
                            results.Add(reader.FileInfo.FileName, data);
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
                                Console.WriteLine("WARNING: Unable to find deduplicated stream for file hash " + hash);
                            }
                            else
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
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Unknown hard link present in TAR archive.");
                        }
                        break;
                    case tar_cs.EntryType.Directory:
                        results.Add(reader.FileInfo.FileName.Trim('/') + "/", null);
                        break;
                    default:
                        Console.WriteLine("WARNING: Ignoring unknown entry type in TAR archive.");
                        break;
                }
            }

            return results;
        }
    }
}

