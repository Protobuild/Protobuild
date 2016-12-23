﻿using System;
using System.IO;
using System.IO.Compression;

namespace Protobuild
{
    internal interface IDeduplicator
    {
        DeduplicatorState CreateState();

        void AddDirectory(DeduplicatorState state, string destinationDirectory);

        void AddFile(DeduplicatorState state, FileInfo sourceFile, string destinationPath);

        void PushToTar(DeduplicatorState state, tar_cs.TarWriter writer);

        void PushToZip(DeduplicatorState state, ZipStorer zip);
    }
}

