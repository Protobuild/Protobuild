using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Protobuild.Tasks;
using fastJSON;

namespace Protobuild
{
    internal class PackageManager : IPackageManager
    {
        private readonly IPackageLookup _packageLookup;

        private readonly IPackageLocator m_PackageLocator;

        private readonly IPackageGlobalTool m_PackageGlobalTool;

        private readonly IPackageRedirector packageRedirector;

        private readonly IFeatureManager _featureManager;

        private readonly IModuleExecution _moduleExecution;
        private readonly IHostPlatformDetector _hostPlatformDetector;

        public const string ARCHIVE_FORMAT_TAR_LZMA = "tar/lzma";

        public const string ARCHIVE_FORMAT_TAR_GZIP = "tar/gzip";

        public const string ARCHIVE_FORMAT_NUGET_ZIP = "nuget/zip";

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
            IModuleExecution moduleExecution,
            IHostPlatformDetector hostPlatformDetector)
        {
            this.packageRedirector = packageRedirector;
            _packageLookup = packageLookup;
            this.m_PackageLocator = packageLocator;
            this.m_PackageGlobalTool = packageGlobalTool;
            _featureManager = featureManager;
            _moduleExecution = moduleExecution;
            _hostPlatformDetector = hostPlatformDetector;
        }

        public void ResolveAll(string workingDirectory, ModuleInfo module, string platform, bool? enableParallelisation, bool forceUpgrade, bool? safeResolve)
        {
            if (!_featureManager.IsFeatureEnabled(Feature.PackageManagement))
            {
                return;
            }

            RedirectableConsole.WriteLine("Starting resolution of packages for " + platform + "...");

            var parallelisation = enableParallelisation ?? _hostPlatformDetector.DetectPlatform() == "Windows";
            if (parallelisation)
            {
                RedirectableConsole.WriteLine("Enabled parallelisation; use --no-parallel to disable...");
            }

            if (module.Packages != null && module.Packages.Count > 0)
            {
                var taskList = new List<Task<Tuple<string, Action>>>();
                var resultList = new List<Tuple<string, Action>>();
                foreach (var submodule in module.Packages)
                {
                    if (submodule.IsActiveForPlatform(platform))
                    {
                        RedirectableConsole.WriteLine("Querying: " + submodule.Uri);
                        var submodule1 = submodule;
                        if (parallelisation)
                        {
                            var task = new Func<Task<Tuple<string, Action>>>(async () =>
                            {
                                var metadata = await Task.Run(() =>
                                    Lookup(workingDirectory, module, submodule1, platform, null, null, forceUpgrade, safeResolve));
                                if (metadata == null)
                                {
                                    return new Tuple<string, Action>(submodule1.Uri, () => { });
                                }
                                return new Tuple<string, Action>(submodule1.Uri,
                                    () => { this.Resolve(workingDirectory, metadata, submodule1, null, null, forceUpgrade, safeResolve); });
                            });
                            taskList.Add(task());
                        }
                        else
                        {
                            var metadata = Lookup(workingDirectory, module, submodule1, platform, null, null, forceUpgrade, safeResolve);
                            if (metadata == null)
                            {
                                resultList.Add(new Tuple<string, Action>(submodule1.Uri, () => { }));
                            }
                            else
                            {
                                resultList.Add(new Tuple<string, Action>(submodule1.Uri,
                                    () => { this.Resolve(workingDirectory, metadata, submodule1, null, null, forceUpgrade, safeResolve); }));
                            }
                        }
                    }
                    else
                    {
                        RedirectableConsole.WriteLine("Skipping resolution for " + submodule.Uri + " because it is not active for this target platform");
                    }
                }

                if (parallelisation)
                {
                    var taskArray = taskList.ToArray();
                    Task.WaitAll(taskArray);
                    foreach (var tuple in taskArray)
                    {
                        RedirectableConsole.WriteLine("Resolving: " + tuple.Result.Item1);
                        tuple.Result.Item2();
                    }
                }
                else
                {
                    foreach (var tuple in resultList)
                    {
                        RedirectableConsole.WriteLine("Resolving: " + tuple.Item1);
                        tuple.Item2();
                    }
                }
            }

            foreach (var submodule in module.GetSubmodules(platform))
            {
                if (submodule.Packages.Count == 0 && submodule.GetSubmodules(platform).Length == 0)
                {
                    if (_featureManager.IsFeatureEnabledInSubmodule(module, submodule, Feature.OptimizationSkipResolutionOnNoPackagesOrSubmodules))
                    {
                        RedirectableConsole.WriteLine(
                            "Skipping package resolution in submodule for " + submodule.Name + " (there are no submodule or packages)");
                        continue;
                    }
                }

                RedirectableConsole.WriteLine(
                    "Invoking package resolution in submodule for " + submodule.Name);
                string parallelMode = null;
                if (_featureManager.IsFeatureEnabledInSubmodule(module, submodule, Feature.TaskParallelisation))
                {
                    if (parallelisation)
                    {
                        parallelMode += "-parallel ";
                    }
                    else
                    {
                        parallelMode += "-no-parallel ";
                    }
                }
                _moduleExecution.RunProtobuild(
                    submodule, 
                    _featureManager.GetFeatureArgumentToPassToSubmodule(module, submodule) + parallelMode +
                    "-resolve " + platform + " " + packageRedirector.GetRedirectionArguments());
                RedirectableConsole.WriteLine(
                    "Finished submodule package resolution for " + submodule.Name);
            }

            RedirectableConsole.WriteLine("Package resolution complete.");
        }

        public void Resolve(string workingDirectory, ModuleInfo module, PackageRef reference, string platform, string templateName, bool? source,
            bool forceUpgrade, bool? safeResolve)
        {
            var metadata = Lookup(workingDirectory, module, reference, platform, templateName, source, forceUpgrade, safeResolve);
            if (metadata == null)
            {
                return;
            }
            Resolve(workingDirectory, metadata, reference, templateName, source, forceUpgrade, safeResolve);
        }

        public IPackageMetadata Lookup(string workingDirectory, ModuleInfo module, PackageRef reference, string platform, string templateName, bool? source,
            bool forceUpgrade, bool? safeResolve)
        {
            if (!_featureManager.IsFeatureEnabled(Feature.PackageManagement))
            {
                return null;
            }

            if (module != null && reference.Folder != null)
            {
                var existingPath = this.m_PackageLocator.DiscoverExistingPackagePath(module.Path, reference, platform);
                if (existingPath != null && Directory.Exists(existingPath))
                {
                    RedirectableConsole.WriteLine("Found an existing working copy of this package at " + existingPath);

                    Directory.CreateDirectory(Path.Combine(workingDirectory, reference.Folder));
                    using (var writer = new StreamWriter(Path.Combine(workingDirectory, reference.Folder, ".redirect")))
                    {
                        writer.WriteLine(existingPath);
                    }

                    return null;
                }
                else
                {
                    if (File.Exists(Path.Combine(workingDirectory, reference.Folder, ".redirect")))
                    {
                        try
                        {
                            File.Delete(Path.Combine(workingDirectory, reference.Folder, ".redirect"));
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
                forceUpgrade,
                reference.IsStaticReference);

            return _packageLookup.Lookup(workingDirectory, request);
        }

        public void Resolve(string workingDirectory, IPackageMetadata metadata, PackageRef reference, string templateName, bool? source,
            bool forceUpgrade, bool? safeResolve)
        {
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
                    Directory.CreateDirectory(Path.Combine(workingDirectory, reference.Folder));

                    if (new DirectoryInfo(Path.Combine(workingDirectory, reference.Folder)).GetFiles().Length > 0 || new DirectoryInfo(Path.Combine(workingDirectory, reference.Folder)).GetDirectories().Length > 0)
                    {
                        if (!File.Exists(Path.Combine(workingDirectory, reference.Folder, ".git")) && !Directory.Exists(Path.Combine(workingDirectory, reference.Folder, ".git")) &&
                            !File.Exists(Path.Combine(workingDirectory, reference.Folder, ".pkg")))
                        {
                            bool shouldSafeResolve;
                            if (safeResolve.HasValue)
                            {
                                // If the user specifies it on the command line, use that setting.
                                shouldSafeResolve = safeResolve.Value;
                            }
                            else
                            {
                                if (!_featureManager.IsFeatureEnabled(Feature.SafeResolutionDisabled))
                                {
                                    // If the module doesn't have this feature set enabled, we default
                                    // to using safe package resolution.
                                    shouldSafeResolve = true;
                                }
                                else
                                {
                                    // If the module does have this feature set enabled, or is using the
                                    // full feature set, we default to turning safe resolution off.
                                    shouldSafeResolve = false;
                                }
                            }

                            if (shouldSafeResolve)
                            {
                                RedirectableConsole.ErrorWriteLine(
                                    "WARNING: The package directory '" + reference.Folder +
                                    "' already exists and contains " +
                                    "files and/or subdirectories, but neither a .pkg file nor a .git file or subdirectory exists.  " +
                                    "This indicates the package directory contains data that is not been instantiated or managed " +
                                    "by Protobuild.  Since there is no safe way to initialize the package in this directory " +
                                    "without a potential loss of data, Protobuild will not modify the contents of this folder " +
                                    "during package resolution.  If the folder does not contains the required package " +
                                    "dependencies, the project generation or build may unexpectedly fail.");
                                return;
                            }
                        }
                    }

                    if (source == null)
                    {
                        if (File.Exists(Path.Combine(workingDirectory, reference.Folder, ".git")) || Directory.Exists(Path.Combine(workingDirectory, reference.Folder, ".git")))
                        {
                            RedirectableConsole.WriteLine("Git repository present at " + Path.Combine(workingDirectory, reference.Folder, ".git") + "; leaving as source version.");
                            source = true;
                        }
                        else
                        {
                            RedirectableConsole.WriteLine("Package type not specified (and no file at " + Path.Combine(workingDirectory, reference.Folder, ".git") + "), requesting binary version.");
                            source = false;
                        }
                    }
                }
            }

            metadata.Resolve(workingDirectory, metadata, reference.Folder, templateName, forceUpgrade, source);
        }
    }
}

