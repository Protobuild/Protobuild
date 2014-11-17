using System;

namespace Protobuild
{
    public interface IAutomaticProjectPackager
    {
        void AutoProject(FileFilter fileFilter, ModuleInfo rootModule, string platform);
    }
}

