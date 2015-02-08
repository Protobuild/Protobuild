using System;

namespace Protobuild
{
    public interface IPackageManager
    {
        void ResolveAll(ModuleInfo module, string platform);

        void Resolve(ModuleInfo module, PackageRef reference, string platform, string templateName, bool? source, bool forceUpgrade = false);
    }
}

