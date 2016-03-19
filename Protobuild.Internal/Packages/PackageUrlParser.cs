using System;
using System.Linq;

namespace Protobuild
{
    internal class PackageUrlParser : IPackageUrlParser
    {
        public PackageRef Parse(string url)
        {
            var branch = "master";
            if (url.LastIndexOf('@') > url.LastIndexOf('/'))
            {
                // A branch / commit ref is specified.
                branch = url.Substring(url.LastIndexOf('@') + 1);
                url = url.Substring(0, url.LastIndexOf('@'));
            }

            var uri = new Uri(url);

            var package = new PackageRef
            {
                Uri = url,
                GitRef = branch,
                Folder = uri.AbsolutePath.Replace("%7C", "/").Replace('|', '/').Trim('/').Split('/').Last()
            };

            return package;
        }
    }
}

