using System;
using System.Collections.Generic;

namespace Protobuild
{
    internal interface IPackageLookup
    {
        IPackageMetadata Lookup(PackageRequestRef request);
    }
}

