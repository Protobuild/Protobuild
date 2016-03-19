using System;

namespace Protobuild
{
    internal interface IModuleUtilities
    {
        string NormalizePlatform(ModuleInfo module, string platform);
    }
}

