using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Text;

namespace Protobuild
{
    public class Deduplicator : IDeduplicator
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
            var sha1 = new SHA1Cng();
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

