using System.Collections.Generic;
using System.IO;

namespace Protobuild
{
    internal class GetRecursiveUtilitiesInPath : IGetRecursiveUtilitiesInPath
    {
        public IEnumerable<string> GetRecursiveFilesInPath(string path)
        {
            var current = new DirectoryInfo(path);

            foreach (var di in current.GetDirectories())
            {
                foreach (string s in GetRecursiveFilesInPath(path + "/" + di.Name))
                {
                    yield return (di.Name + "/" + s).Trim('/');
                }
            }

            foreach (var fi in current.GetFiles())
            {
                yield return fi.Name;
            }
        }
    }
}

