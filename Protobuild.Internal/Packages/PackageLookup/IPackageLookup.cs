using System;
using System.Collections.Generic;

namespace Protobuild
{
    public interface IPackageLookup
    {
        IPackageMetadata Lookup(PackageRequestRef request);
    }
}

