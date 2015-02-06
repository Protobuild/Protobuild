using System;

namespace Protobuild
{
    public interface IPackageManager
    {
        void ResolveAll(ModuleInfo module, string platform);

        void Resolve(PackageRef reference, string platform, string templateName, bool? source);
    }
}

