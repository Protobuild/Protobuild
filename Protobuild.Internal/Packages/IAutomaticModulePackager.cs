using System;

namespace Protobuild
{
    public interface IAutomaticModulePackager
    {
        void Autopackage(
            FileFilter fileFilter,
            Execution execution, 
            ModuleInfo rootModule, 
            string rootPath,
            string platform);
    }
}

