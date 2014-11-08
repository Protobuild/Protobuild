using System;
using System.Collections.Generic;

namespace Protobuild
{
    public interface IJSILProvider
    {
        bool GetJSIL(out string jsilDirectory, out string jsilCompilerFile);

        IEnumerable<KeyValuePair<string, string>> GetJSILLibraries();
    }
}

