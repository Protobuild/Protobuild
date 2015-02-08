using System;

namespace Protobuild
{
    public static class PathUtils
    {
        public static string GetRelativePath(string absoluteFrom, string absoluteTo)
        {
            absoluteFrom = absoluteFrom.Replace('\\', '/');
            absoluteTo = absoluteTo.Replace('\\', '/');
            return (new Uri(absoluteFrom).MakeRelativeUri(new Uri(absoluteTo)))
                .ToString().Replace('/', '\\');
        }
    }
}

