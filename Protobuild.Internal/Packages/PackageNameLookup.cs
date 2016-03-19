using System;
using System.Linq;

namespace Protobuild
{
    internal class PackageNameLookup : IPackageNameLookup
    {
        public PackageRef LookupPackageByName(ModuleInfo module, string url)
        {
            var packages = module.Packages.Where(x => x.Uri.Contains(url)).ToArray();

            PackageRef packageRef;
            if (packages.Length == 1)
            {
                packageRef = packages[0];
            }
            else if (packages.Length == 0)
            {
                throw new InvalidOperationException("No such package has been added");
            }
            else
            {
                if (packages.Any(x => x.Uri == url))
                {
                    packageRef = packages.First(x => x.Uri == url);
                }
                else
                {
                    throw new InvalidOperationException("Package reference is ambigious");
                }
            }

            return packageRef;
        }
    }
}
