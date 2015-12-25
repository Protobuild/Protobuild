using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using Protobuild.Tasks;
using fastJSON;

namespace Protobuild
{
    public class PackageManager : IPackageManager
    {
        private readonly IPackageLookup _packageLookup;

        private readonly IPackageLocator m_PackageLocator;

        private readonly IPackageGlobalTool m_PackageGlobalTool;

        private readonly IPackageRedirector packageRedirector;

        private readonly IFeatureManager _featureManager;

        private readonly IModuleExecution _moduleExecution;

        public const string ARCHIVE_FORMAT_TAR_LZMA = "tar/lzma";

        public const string ARCHIVE_FORMAT_TAR_GZIP = "tar/gzip";

        public const string PACKAGE_TYPE_LIBRARY = "library";

        public const string PACKAGE_TYPE_TEMPLATE = "template";

        public const string PACKAGE_TYPE_GLOBAL_TOOL = "global-tool";

        public const string SOURCE_FORMAT_GIT = "git";

        public const string SOURCE_FORMAT_DIRECTORY = "directory";

        public PackageManager(
            IPackageLookup packageLookup,
            IPackageLocator packageLocator,
            IPackageGlobalTool packageGlobalTool,
            IPackageRedirector packageRedirector,
            IFeatureManager featureManager,
            IModuleExecution moduleExecution)
        {
            this.packageRedirector = packageRedirector;
            _packageLookup = packageLookup;
            this.m_PackageLocator = packageLocator;
            this.m_PackageGlobalTool = packageGlobalTool;
            _featureManager = featureManager;
            _moduleExecution = moduleExecution;
        }

        public void ResolveAll(ModuleInfo module, string platform)
        {
            if (!_featureManager.IsFeatureEnabled(Feature.PackageManagement))
            {
                return;
            }

            Console.WriteLine("Starting resolution of packages for " + platform + "...");

            if (module.Packages != null && module.Packages.Count > 0)
            {
                foreach (var submodule in module.Packages)
                {
                    if (submodule.IsActiveForPlatform(platform))
                    {
                        Console.WriteLine("Resolving: " + submodule.Uri);
                        this.Resolve(module, submodule, platform, null, null);
                    }
                    else
                    {
                        Console.WriteLine("Skipping resolution for " + submodule.Uri + " because it is not active for this target platform");
                    }
                }
            }

            foreach (var submodule in module.GetSubmodules(platform))
            {
                if (submodule.Packages.Count == 0 && submodule.GetSubmodules(platform).Length == 0)
                {
                    if (_featureManager.IsFeatureEnabledInSubmodule(module, submodule, Feature.OptimizationSkipResolutionOnNoPackagesOrSubmodules))
                    {
                        Console.WriteLine(
                            "Skipping package resolution in submodule for " + submodule.Name + " (there are no submodule or packages)");
                        continue;
                    }
                }

                Console.WriteLine(
                    "Invoking package resolution in submodule for " + submodule.Name);
                _moduleExecution.RunProtobuild(
                    submodule, 
                    _featureManager.GetFeatureArgumentToPassToSubmodule(module, submodule) + 
                    "-resolve " + platform + " " + packageRedirector.GetRedirectionArguments());
                Console.WriteLine(
                    "Finished submodule package resolution for " + submodule.Name);
            }

            Console.WriteLine("Package resolution complete.");
        }

        public void Resolve(ModuleInfo module, PackageRef reference, string platform, string templateName, bool? source, bool forceUpgrade = false)
        {
            if (!_featureManager.IsFeatureEnabled(Feature.PackageManagement))
            {
                return;
            }

            if (module != null && reference.Folder != null)
            {
                var existingPath = this.m_PackageLocator.DiscoverExistingPackagePath(module.Path, reference, platform);
                if (existingPath != null && Directory.Exists(existingPath))
                {
                    Console.WriteLine("Found an existing working copy of this package at " + existingPath);

                    Directory.CreateDirectory(reference.Folder);
                    using (var writer = new StreamWriter(Path.Combine(reference.Folder, ".redirect")))
                    {
                        writer.WriteLine(existingPath);
                    }

                    return;
                }
                else
                {
                    if (File.Exists(Path.Combine(reference.Folder, ".redirect")))
                    {
                        try
                        {
                            File.Delete(Path.Combine(reference.Folder, ".redirect"));
                        }
                        catch
                        {
                        }
                    }
                }
            }

            var request = new PackageRequestRef(
                reference.Uri,
                reference.GitRef,
                platform,
                !forceUpgrade && reference.IsCommitReference);
            
            var metadata = _packageLookup.Lookup(request);
            
            string toolFolder = null;
            if (reference.Folder == null)
            {
                if (metadata.PackageType == PACKAGE_TYPE_GLOBAL_TOOL)
                {
                }
                else
                {
                    throw new InvalidOperationException(
                        "No target folder was provided for package resolution, and the resulting package is not " +
                        "a global tool.");
                }
            }
            else
            {
                if (metadata.PackageType == PackageManager.PACKAGE_TYPE_TEMPLATE && templateName == null)
                {
                    throw new InvalidOperationException(
                        "Template referenced as part of module packages.  Templates can only be used " +
                        "with the --start option.");
                }
                else if (metadata.PackageType == PackageManager.PACKAGE_TYPE_LIBRARY)
                {
                    Directory.CreateDirectory(reference.Folder);

                    if (new DirectoryInfo(reference.Folder).GetFiles().Length > 0 || new DirectoryInfo(reference.Folder).GetDirectories().Length > 0)
                    {
                        if (!File.Exists(Path.Combine(reference.Folder, ".git")) && !Directory.Exists(Path.Combine(reference.Folder, ".git")) &&
                            !File.Exists(Path.Combine(reference.Folder, ".pkg")))
                        {
                            Console.Error.WriteLine(
                                "WARNING: The package directory '" + reference.Folder + "' already exists and contains " +
                                "files and/or subdirectories, but neither a .pkg file nor a .git file or subdirectory exists.  " +
                                "This indicates the package directory contains data that is not been instantiated or managed " +
                                "by Protobuild.  Since there is no safe way to initialize the package in this directory " +
                                "without a potential loss of data, Protobuild will not modify the contents of this folder " +
                                "during package resolution.  If the folder does not contains the required package " +
                                "dependencies, the project generation or build may unexpectedly fail.");
                            return;
                        }
                    }

                    if (source == null)
                    {
                        if (File.Exists(Path.Combine(reference.Folder, ".git")) || Directory.Exists(Path.Combine(reference.Folder, ".git")))
                        {
                            Console.WriteLine("Git repository present at " + Path.Combine(reference.Folder, ".git") + "; leaving as source version.");
                            source = true;
                        }
                        else
                        {
                            Console.WriteLine("Package type not specified (and no file at " + Path.Combine(reference.Folder, ".git") + "), requesting binary version.");
                            source = false;
                        }
                    }
                }
            }

            metadata.Resolve(metadata, reference.Folder, templateName, forceUpgrade, source);
        }
    }
}

