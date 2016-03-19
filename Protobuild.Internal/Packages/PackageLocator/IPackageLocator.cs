using System;

namespace Protobuild
{
    internal interface IPackageLocator
    {
        string DiscoverExistingPackagePath(string moduleRoot, PackageRef package, string platform);
    }
}

