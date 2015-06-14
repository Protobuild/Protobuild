using System;
using System.Linq;

namespace Protobuild
{
    public class PackageUrlParser : IPackageUrlParser
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
                Folder = uri.AbsolutePath.Trim('/').Split('/').Last()
            };

            // Strip an encoded | character if it is present.
            if (package.Folder.StartsWith(@"%7C", StringComparison.InvariantCulture))
            {
                package.Folder = package.Folder.Substring("%7C".Length);
            }

            return package;
        }
    }
}

