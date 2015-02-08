using System;
using System.IO;
using System.Linq;

namespace Protobuild
{
    public class PackageLocator : IPackageLocator
    {
        public string DiscoverExistingPackagePath(string moduleRoot, PackageRef package)
        {
            // Check the ModuleInfo.xml files of other modules in the
            // hierarchy in this order:
            // * Above us (this ensures the common libraries are at the highest point)
            // * Across from us (in the same folder) up to our name alphabetically
            // * Below us

            var parentModule = this.GetParentModule(moduleRoot);
            if (parentModule != null)
            {
                var above = this.CheckAbove(parentModule, package);
                if (above != null)
                {
                    return above;
                }

                var directoryName = new DirectoryInfo(moduleRoot).Name;
                var across = this.CheckAcross(parentModule, package, directoryName);
                if (across != null)
                {
                    return across;
                }
            }

            return null;
        }

        private string CheckAbove(string modulePath, PackageRef package)
        {
            var module = ModuleInfo.Load(Path.Combine(modulePath, "Build", "Module.xml"));
            var found = module.Packages.Select(x => (PackageRef?)x)
                .FirstOrDefault(x => string.Compare(x.Value.Uri, package.Uri, StringComparison.InvariantCulture) == 0);
            if (found != null)
            {
                return Path.Combine(modulePath, found.Value.Folder);
            }

            var parent = this.GetParentModule(modulePath);
            if (parent != null)
            {
                var above = this.CheckAbove(parent, package);
                if (above != null)
                {
                    return above;
                }

                var directoryName = new DirectoryInfo(modulePath).Name;
                var across = this.CheckAcross(parent, package, directoryName);
                if (across != null)
                {
                    return across;
                }
            }

            return null;
        }

        private string CheckAcross(string modulePath, PackageRef package, string originalDirectoryName)
        {
            var directory = new DirectoryInfo(modulePath);
            foreach (var subdirectory in directory.GetDirectories())
            {
                if (string.Compare(subdirectory.Name, originalDirectoryName, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    // This directory is either the original directory or
                    // a directory with a later alphabetical order.  We ignore
                    // it (this is how we resolve whether A should use B's package
                    // or B should use A's package).
                    continue;
                }

                if (File.Exists(Path.Combine(subdirectory.FullName, "Build", "Module.xml")))
                {
                    var module = ModuleInfo.Load(Path.Combine(subdirectory.FullName, "Build", "Module.xml"));
                    var found = module.Packages.Select(x => (PackageRef?)x)
                        .FirstOrDefault(x => string.Compare(x.Value.Uri, package.Uri, StringComparison.InvariantCulture) == 0);
                    if (found != null)
                    {
                        return Path.Combine(subdirectory.FullName, found.Value.Folder);
                    }

                    var submodules = module.GetSubmodules();
                    foreach (var submodule in submodules)
                    {
                        var below = this.CheckBelow(submodule.Path, package);
                        if (below != null)
                        {
                            return below;
                        }
                    }
                }
            }

            return null;
        }

        private string CheckBelow(string modulePath, PackageRef package)
        {
            var module = ModuleInfo.Load(Path.Combine(modulePath, "Build", "Module.xml"));
            var found = module.Packages.Select(x => (PackageRef?)x)
                .FirstOrDefault(x => string.Compare(x.Value.Uri, package.Uri, StringComparison.InvariantCulture) == 0);
            if (found != null)
            {
                return Path.Combine(modulePath, found.Value.Folder);
            }

            var submodules = module.GetSubmodules();
            foreach (var submodule in submodules)
            {
                var below = this.CheckBelow(submodule.Path, package);
                if (below != null)
                {
                    return below;
                }
            }

            return null;
        }

        private string GetParentModule(string modulePath)
        {
            var parentDirectory = Path.Combine(modulePath, "..");
            if (Directory.Exists(Path.Combine(parentDirectory, "Build")) &&
                File.Exists(Path.Combine(parentDirectory, "Build", "Module.xml")) &&
                File.Exists(Path.Combine(parentDirectory, "Protobuild.exe")))
            {
                return parentDirectory;
            }

            return null;
        }
    }
}

