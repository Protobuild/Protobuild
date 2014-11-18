using System;
using System.Collections.Generic;
using System.IO;

namespace Protobuild
{
    public interface IFileFilterParser
    {
        void ParseAndApply(FileFilter result, Stream inputFilterFile, Dictionary<string, Action<FileFilter>> customDirectives);
    }
}

