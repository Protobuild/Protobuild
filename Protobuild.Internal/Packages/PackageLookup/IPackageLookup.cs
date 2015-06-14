using System;
using System.Collections.Generic;

namespace Protobuild
{
    public interface IPackageLookup
    {
        void Lookup(
            string uri,
            string platform,
            bool preferCacheLookup,
            out string sourceUri, 
            out string type,
            out Dictionary<string, string> downloadMap,
            out Dictionary<string, string> archiveTypeMap,
            out Dictionary<string, string> resolvedHash,
            out IPackageTransformer transformer);
    }
}

