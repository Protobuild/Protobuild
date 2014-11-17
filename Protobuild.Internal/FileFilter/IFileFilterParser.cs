using System;
using System.Collections.Generic;

namespace Protobuild
{
    public interface IFileFilterParser
    {
        FileFilter Parse(ModuleInfo rootModule, string platform, string path, IEnumerable<string> filenames);
    }
}

