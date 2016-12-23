using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.IO.Compression;

namespace Protobuild
{
    internal class Deduplicator : IDeduplicator
    {
        public DeduplicatorState CreateState()
        {
            return new DeduplicatorState
            {
                FileHashToSource = new Dictionary<string, Stream>(),
                DestinationToFileHash = new Dictionary<string, string>(),
            };
        }

        public void AddDirectory(DeduplicatorState state, string destinationDirectory)
        {
            state.DestinationToFileHash.Add(destinationDirectory, null);
        }

        public void AddFile(DeduplicatorState state, FileInfo sourceFile, string destinationPath)
        {
            if (state.DestinationToFileHash.ContainsKey(destinationPath))
            {
                // File has already been added.
                return;
            }

            // Read the source file.
            var memory = new MemoryStream();
            using (var stream = new BufferedStream(new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.None), 1200000))
            {
                stream.CopyTo(memory);
            }

            // Hash the memory stream.
            var sha1 = new SHA1Managed();
            memory.Seek(0, SeekOrigin.Begin);
            var hashBytes = sha1.ComputeHash(memory);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            memory.Seek(0, SeekOrigin.Begin);

            // Add to the file hash -> source map if not already present.
            if (!state.FileHashToSource.ContainsKey(hashString))
            {
                state.FileHashToSource.Add(hashString, memory);
            }
            else
            {
                memory.Dispose();
            }

            state.DestinationToFileHash.Add(destinationPath, hashString);
        }

        public void PushToZip(DeduplicatorState state, ZipStorer zip)
        {
            // All files that don't reside under protobuild/ have to be
            // maintained intact for NuGet to work properly.
            var rootCopies = new Dictionary<string, string>();
            foreach (var kv in state.DestinationToFileHash)
            {
                if (kv.Key.ToLowerInvariant().Replace('\\', '/').StartsWith("protobuild/"))
                {
                    // This file can be deduplicated.
                }
                else
                {
                    // This file can not be deduplicated and must reside in place.  We keep
                    // a dictionary in memory that allows the file at it's currently location
                    // to be the deduplication target for any files under protobuild/ that match,
                    // instead of also storing a copy under _DedupFiles.
                    if (kv.Value == null)
                    {
                        // Directory; ignore this since they are automatically recreated by the
                        // reduplicator.
                        continue;
                    }

                    rootCopies.Add(kv.Value, kv.Key);
                    zip.AddStream(
                        ZipStorer.Compression.Deflate,
                        kv.Key,
                        state.FileHashToSource[kv.Value],
                        DateTime.UtcNow,
                        string.Empty,
                        true);
                }
            }

            // Now store any files that weren't stored in the package root, into a
            // _DedupFiles folder.
            foreach (var kv in state.FileHashToSource)
            {
                if (rootCopies.ContainsKey(kv.Key))
                {
                    // This was stored in the root of the package already.
                }
                else
                {
                    // We need to store this under _DedupFiles.
                    zip.AddStream(
                        ZipStorer.Compression.Deflate,
                        "_DedupFiles/" + kv.Key,
                        kv.Value,
                        DateTime.UtcNow,
                        string.Empty,
                        true);
                }
            }

            // Now write out the text file that has the mappings.  While in a tar/gz
            // file we use symlinks, those aren't available for ZIP packages, so we 
            // need a text file in the root of the package that describes path to path
            // mappings.
            using (var mappingStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(mappingStream, Encoding.UTF8, 4096, true))
                {
                    foreach (var kv in state.DestinationToFileHash)
                    {
                        if (kv.Value == null)
                        {
                            // Directory.
                            writer.WriteLine(kv.Key + "?<DIRECTORY>");
                            continue;
                        }

                        if (rootCopies.ContainsKey(kv.Value))
                        {
                            writer.WriteLine(kv.Key + "?" + rootCopies[kv.Value]);
                        }
                        else
                        {
                            writer.WriteLine(kv.Key + "?_DedupFiles/" + kv.Value);
                        }
                    }

                    writer.Flush();

                    mappingStream.Seek(0, SeekOrigin.Begin);

                    zip.AddStream(
                        ZipStorer.Compression.Deflate,
                        "_DedupIndex.txt",
                        mappingStream,
                        DateTime.UtcNow,
                        "Deduplication Index Lookup");
                }
            }
        }

        public void PushToTar(DeduplicatorState state, tar_cs.TarWriter writer)
        {
            const int ReadWriteExecuteAllUsers = 511;

            // First create all the entries for the actual file streams, based
            // on their hashes.
            writer.WriteDirectoryEntry(
                "_DedupFiles", 
                "default", 
                "default", 
                ReadWriteExecuteAllUsers, 
                DateTime.UtcNow);
            foreach (var kv in state.FileHashToSource)
            {
                writer.WriteFile(
                    kv.Value, 
                    kv.Value.Length, 
                    "_DedupFiles/" + kv.Key, 
                    "default", 
                    "default", 
                    ReadWriteExecuteAllUsers, 
                    DateTime.UtcNow);
            }

            // Now write all of the real directories and their symlinks.
            foreach (var kv in state.DestinationToFileHash.OrderBy(kv => kv.Key))
            {
                if (kv.Key.EndsWith("/"))
                {
                    // Directory
                    writer.WriteDirectoryEntry(
                        kv.Key.Replace('\\', '/').TrimEnd('/'),
                        "default",
                        "default",
                        ReadWriteExecuteAllUsers,
                        DateTime.UtcNow);
                }
                else
                {
                    // File
                    writer.WriteHardLink(
                        "_DedupFiles/" + kv.Value,
                        kv.Key.Replace('\\', '/'),
                        "default",
                        "default",
                        ReadWriteExecuteAllUsers,
                        DateTime.UtcNow);
                }
            }
        }
    }
}

