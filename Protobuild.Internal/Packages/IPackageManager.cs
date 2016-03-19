using System;

namespace Protobuild
{
    internal interface IPackageManager
    {
        void ResolveAll(ModuleInfo module, string platform, bool? enableParallelisation, bool forceUpgrade = false);

        IPackageMetadata Lookup(ModuleInfo module, PackageRef reference, string platform, string templateName,
            bool? source,
            bool forceUpgrade = false);

        void Resolve(IPackageMetadata metadata, PackageRef reference, string templateName, bool? source,
            bool forceUpgrade = false);

        void Resolve(ModuleInfo module, PackageRef reference, string platform, string templateName, bool? source, bool forceUpgrade = false);
    }
}

