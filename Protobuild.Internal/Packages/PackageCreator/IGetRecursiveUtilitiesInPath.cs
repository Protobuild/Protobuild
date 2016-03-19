using System.Collections.Generic;

namespace Protobuild
{
    internal interface IGetRecursiveUtilitiesInPath
    {
        IEnumerable<string> GetRecursiveFilesInPath(string path);
    }
}

