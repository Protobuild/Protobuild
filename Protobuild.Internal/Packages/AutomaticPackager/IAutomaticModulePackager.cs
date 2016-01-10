using System;
using System.Collections.Generic;

namespace Protobuild
{
    public interface IAutomaticModulePackager
    {
        void Autopackage(
            FileFilter fileFilter,
            Execution execution, 
            ModuleInfo rootModule, 
            string rootPath,
            string platform,
            List<string> temporaryFiles);
    }
}

