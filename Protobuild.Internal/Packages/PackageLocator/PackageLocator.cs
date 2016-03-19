using System;
using System.IO;
using System.Linq;

namespace Protobuild
{
    internal class PackageLocator : IPackageLocator
    {
        private readonly IPackageRedirector _packageRedirector;

        public PackageLocator(IPackageRedirector packageRedirector)
        {
            _packageRedirector = packageRedirector;
        }

        public string DiscoverExistingPackagePath(string moduleRoot, PackageRef package, string platform)
        {
            var redirectedUri = _packageRedirector.RedirectPackageUrl(package.Uri);
            if (redirectedUri.StartsWith("local-pointer://", StringComparison.InvariantCultureIgnoreCase))
            {
                // This is a locally redirected package, where the redirect goes straight to another folder
                // on the local computer (alternatively local-git:// will still clone from the target
                // folder, while this allows you to work on the target folder directly).
                return redirectedUri.Substring("local-pointer://".Length);
            }

            // Check the ModuleInfo.xml files of other modules in the
            // hierarchy in this order:
            // * Above us (this ensures the common libraries are at the highest point)
            // * Across from us (in the same folder) up to our name alphabetically
            // * Below us

            bool isNestedInPlatformFolder;
            var parentModule = this.GetParentModule(moduleRoot, platform, out isNestedInPlatformFolder);
            if (parentModule != null)
            {
                var above = this.CheckAbove(parentModule, package, platform);
                if (above != null)
                {
                    return above;
                }

                var directoryName = new DirectoryInfo(moduleRoot).Name;
                if (isNestedInPlatformFolder)
                {
                    directoryName = new DirectoryInfo(Path.Combine(moduleRoot, "..")).Name;
                }

                var across = this.CheckAcross(parentModule, package, directoryName, platform);
                if (across != null)
                {
                    return across;
                }
            }

            return null;
        }

        private string CheckAbove(string modulePath, PackageRef package, string platform)
        {
            var module = ModuleInfo.Load(Path.Combine(modulePath, "Build", "Module.xml"));
            var found = module.Packages.Select(x => (PackageRef?)x)
                .FirstOrDefault(x => string.Compare(x.Value.Uri, package.Uri, StringComparison.InvariantCulture) == 0);
            if (found != null)
            {
                return Path.Combine(modulePath, found.Value.Folder);
            }

            bool isNestedInPlatformFolder;
            var parent = this.GetParentModule(modulePath, platform, out isNestedInPlatformFolder);
            if (parent != null)
            {
                var above = this.CheckAbove(parent, package, platform);
                if (above != null)
                {
                    return above;
                }

                var directoryName = new DirectoryInfo(modulePath).Name;
                if (isNestedInPlatformFolder)
                {
                    directoryName = new DirectoryInfo(Path.Combine(modulePath, "..")).Name;
                }

                var across = this.CheckAcross(parent, package, directoryName, platform);
                if (across != null)
                {
                    return across;
                }
            }

            return null;
        }

        private string CheckAcross(string modulePath, PackageRef package, string originalDirectoryName, string platform)
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
                        var below = this.CheckBelow(submodule.Path, package, platform);
                        if (below != null)
                        {
                            return below;
                        }
                    }
                }

                if (File.Exists(Path.Combine(subdirectory.FullName, platform, "Build", "Module.xml")))
                {
                    var module = ModuleInfo.Load(Path.Combine(subdirectory.FullName, platform, "Build", "Module.xml"));
                    var found = module.Packages.Select(x => (PackageRef?)x)
                        .FirstOrDefault(x => string.Compare(x.Value.Uri, package.Uri, StringComparison.InvariantCulture) == 0);
                    if (found != null)
                    {
                        return Path.Combine(subdirectory.FullName, platform, found.Value.Folder);
                    }

                    var submodules = module.GetSubmodules();
                    foreach (var submodule in submodules)
                    {
                        var below = this.CheckBelow(submodule.Path, package, platform);
                        if (below != null)
                        {
                            return below;
                        }
                    }
                }
            }

            return null;
        }

        private string CheckBelow(string modulePath, PackageRef package, string platform)
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
                var below = this.CheckBelow(submodule.Path, package, platform);
                if (below != null)
                {
                    return below;
                }
            }

            return null;
        }

        private string GetParentModule(string modulePath, string platform, out bool isNestedInPlatformFolder)
        {
            isNestedInPlatformFolder = false;

            var parentDirectory = Path.Combine(modulePath, "..");
            if (Directory.Exists(Path.Combine(parentDirectory, "Build")) &&
                File.Exists(Path.Combine(parentDirectory, "Build", "Module.xml")) &&
                File.Exists(Path.Combine(parentDirectory, "Protobuild.exe")))
            {
                return parentDirectory;
            }

            if (string.Compare(new DirectoryInfo(modulePath).Name, platform, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // Our parent directory is named the same as a platform, we might be
                // inside a binary package, so check the parent of our parent folder to
                // see if that's a valid module as well.
                parentDirectory = Path.Combine(parentDirectory, "..");
                if (Directory.Exists(Path.Combine(parentDirectory, "Build")) &&
                    File.Exists(Path.Combine(parentDirectory, "Build", "Module.xml")) &&
                    File.Exists(Path.Combine(parentDirectory, "Protobuild.exe")))
                {
                    isNestedInPlatformFolder = true;
                    return parentDirectory;
                }
            }

            return null;
        }
    }
}

