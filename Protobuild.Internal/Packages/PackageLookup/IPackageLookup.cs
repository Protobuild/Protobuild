using System;
using System.Collections.Generic;

namespace Protobuild
{
    internal interface IPackageLookup
    {
        IPackageMetadata Lookup(string workingDirectory, PackageRequestRef request);
    }
}

