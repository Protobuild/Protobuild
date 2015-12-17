using System;

namespace Protobuild
{
    public interface IModuleUtilities
    {
        string NormalizePlatform(ModuleInfo module, string platform);
    }
}

