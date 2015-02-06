using System;
using System.IO;

namespace Protobuild
{
    public interface IDeduplicator
    {
        DeduplicatorState CreateState();

        void AddDirectory(DeduplicatorState state, string destinationDirectory);

        void AddFile(DeduplicatorState state, FileInfo sourceFile, string destinationPath);

        void PushToTar(DeduplicatorState state, tar_cs.TarWriter writer);
    }
}

