using System;
using System.Collections.Generic;

namespace Protobuild
{
    internal interface IAutomaticModulePackager
    {
        void Autopackage(
            string workingDirectory,
            FileFilter fileFilter,
            Execution execution, 
            ModuleInfo rootModule, 
            string rootPath,
            string platform,
            string packageFormat,
            List<string> temporaryFiles);
    }
}

