using System;
using System.Collections.Generic;
using System.IO;

namespace Protobuild
{
    internal class DeduplicatorState
    {
        public Dictionary<string, string> DestinationToFileHash { get; set; }

        public Dictionary<string, Stream> FileHashToSource { get; set; }
    }
}

