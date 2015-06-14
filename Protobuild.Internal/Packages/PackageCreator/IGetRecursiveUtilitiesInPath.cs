using System.Collections.Generic;

namespace Protobuild
{
    public interface IGetRecursiveUtilitiesInPath
    {
        IEnumerable<string> GetRecursiveFilesInPath(string path);
    }
}

