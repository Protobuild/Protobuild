using System;
using System.Linq;

namespace Protobuild
{
    internal class PackageUrlParser : IPackageUrlParser
    {
        public PackageRef Parse(string url)
        {
            var branch = "master";
            if (url.LastIndexOf('@') > url.LastIndexOf('/') || (url.IndexOf('@') > 0 && url.IndexOf('/') == -1))
            {
                // A branch / commit ref is specified.
                branch = url.Substring(url.LastIndexOf('@') + 1);
                url = url.Substring(0, url.LastIndexOf('@'));
            }

            try
            {
                var uri = new Uri(url);
                var folder = uri.AbsolutePath.Replace("%7C", "/").Replace('|', '/').Trim('/').Split('/').Last();
                if (folder.EndsWith(".nupkg"))
                {
                    folder = folder.Substring(0, folder.Length - ".nupkg".Length);
                }

                var package = new PackageRef
                {
                    Uri = url,
                    GitRef = branch,
                    Folder = folder
                };

                return package;
            }
            catch (UriFormatException)
            {
                if (url.IndexOf("/") == -1)
                {
                    // This is just a package name, not a full URI.  Treat it as a NuGet v3 package reference.
                    return new PackageRef
                    {
                        Uri = "https-nuget-v3://api.nuget.org/v3/index.json|" + url,
                        GitRef = branch,
                        Folder = url
                    };
                }

                throw;
            }
        }
    }
}

