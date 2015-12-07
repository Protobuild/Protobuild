﻿using System;
using System.Linq;
using System.IO;
using Protobuild.Tasks;

namespace Protobuild
{
    public class ActionDispatch : IActionDispatch
    {
        private readonly LightweightKernel m_LightweightKernel;

        private readonly IHostPlatformDetector m_HostPlatformDetector;

        private readonly IPackageManager m_PackageManager;

        public ActionDispatch(
            LightweightKernel lightweightKernel,
            IHostPlatformDetector hostPlatformDetector,
            IPackageManager packageManager)
        {
            this.m_LightweightKernel = lightweightKernel;
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_PackageManager = packageManager;
        }

        /// <summary>
        /// Performs a resynchronisation, synchronisation, generation or clean on the specified module.
        /// </summary>
        /// <returns><c>true</c>, if the action succeeded, <c>false</c> otherwise.</returns>
        /// <param name="module">The module to perform the action on.</param>
        /// <param name="action">The action to perform, either "resync", "sync", "generate" or "clean".</param>
        /// <param name="platform">The platform to perform the action for.</param>
        /// <param name="enabledServices">A list of enabled services.</param>
        /// <param name="disabledServices">A list of disabled services.</param>
        /// <param name="serviceSpecPath">The service specification path.</param>
        /// <param name="debugServiceResolution">Whether to enable debugging information during service resolution.</param>
        /// <param name="disablePackageResolution">Whether to disable package resolution.</param>
        /// <param name="disableHostPlatformGeneration">Whether to disable generation of the host platform projects.</param>
        public bool PerformAction(
            ModuleInfo module, 
            string action, 
            string platform = null, 
            string[] enabledServices = null, 
            string[] disabledServices = null, 
            string serviceSpecPath = null,
            bool debugServiceResolution = false,
            bool disablePackageResolution = false,
            bool disableHostPlatformGeneration = false)
        {
            var platformSupplied = !string.IsNullOrWhiteSpace(platform);

            var hostPlatform = this.m_HostPlatformDetector.DetectPlatform();
            if (string.IsNullOrWhiteSpace(platform))
            {
                platform = hostPlatform;
            }

            var originalPlatform = platform;
            string primaryPlatform = null;
            string multiplePlatforms = null;

            if (platform.Contains(","))
            {
                // The user requested multiple platforms; the first one
                // is the primary platform.
                var platforms = platform.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (platforms.Length == 0)
                {
                    Console.Error.WriteLine("You supplied only commas where a list of platforms was expected.");
                    ExecEnvironment.Exit(1);
                    return false;
                }
                else
                {
                    for (var i = 0; i < platforms.Length; i++)
                    {
						var newPlatform = module.NormalizePlatform(platforms[i]);
						if (newPlatform == null)
                        {
                            ShowSupportedPlatformsError(module, platforms[i]);
                            return false;
                        }
						platforms[i] = newPlatform;
                    }

                    primaryPlatform = platforms[0];
                    multiplePlatforms = platforms.Aggregate((a, b) => a + "," + b);
                }
            }
            else
            {
                platform = module.NormalizePlatform(platform);

                if (platform == null && !platformSupplied)
                {
                    // The current host platform isn't supported, so we shouldn't try to
                    // operate on it.
                    string firstPlatform = null;
                    switch (this.m_HostPlatformDetector.DetectPlatform())
                    {
                        case "Windows":
                            firstPlatform = module.DefaultWindowsPlatforms.Split(',').FirstOrDefault();
                            break;
                        case "MacOS":
                            firstPlatform = module.DefaultMacOSPlatforms.Split(',').FirstOrDefault();
                            break;
                        case "Linux":
                            firstPlatform = module.DefaultLinuxPlatforms.Split(',').FirstOrDefault();
                            break;
                    }

                    if (firstPlatform != null)
                    {
                        // This will end up null if the first platform isn't supported
                        // either and hence throw the right message.
                        platform = module.NormalizePlatform(firstPlatform);
                    }
                }

                if (platform == null)
                {
					ShowSupportedPlatformsError(module, originalPlatform);
                    return false;
                }

                primaryPlatform = platform;
            }

            // You can generate multiple targets by default by setting the <DefaultWindowsPlatforms>
            // <DefaultMacOSPlatforms> and <DefaultLinuxPlatforms> tags in Module.xml.  Note that
            // synchronisation will only be done for the primary platform, as there is no correct
            // synchronisation behaviour when dealing with multiple C# projects.
            //
            // We only trigger this behaviour when the platform is omitted; if you explicitly
            // specify "Windows" on the command line, we'll only generate / resync / sync
            // the Windows platform.
            if (!platformSupplied)
            {
                switch (platform)
                {
                    case "Windows":
                        multiplePlatforms = module.DefaultWindowsPlatforms;
                        break;
                    case "MacOS":
                        multiplePlatforms = module.DefaultMacOSPlatforms;
                        break;
                    case "Linux":
                        multiplePlatforms = module.DefaultLinuxPlatforms;
                        break;
                }
            }

            // If no overrides are set, just use the current platform.
            if (string.IsNullOrEmpty(multiplePlatforms))
            {
                multiplePlatforms = platform;
            }

            // Resolve submodules as needed.
            if (!disablePackageResolution)
            {
                this.m_PackageManager.ResolveAll(module, primaryPlatform);
            }

            // You can configure the default action for Protobuild in their project
            // with the <DefaultAction> tag in Module.xml.  If omitted, default to a resync.
            // Valid options for this tag are either "Generate", "Resync" or "Sync".

            // If the actions are "Resync" or "Sync", then we need to perform an initial
            // step against the primary platform.
            switch (action.ToLower())
            {
                case "generate":
                    if (!this.GenerateProjectsForPlatform(
                        module, 
                        primaryPlatform,
                        enabledServices,
                        disabledServices,
                        serviceSpecPath, 
                        debugServiceResolution,
                        disablePackageResolution,
                        disableHostPlatformGeneration))
                    {
                        return false;
                    }

                    break;
                case "resync":
                    if (!this.ResyncProjectsForPlatform(
                        module, 
                        primaryPlatform, 
                        enabledServices, 
                        disabledServices, 
                        serviceSpecPath, 
                        debugServiceResolution,
                        disablePackageResolution,
                        disableHostPlatformGeneration))
                    {
                        return false;
                    }

                    break;
                case "sync":
                    return this.SyncProjectsForPlatform(module, primaryPlatform);
                case "clean":
                    if (!this.CleanProjectsForPlatform(module, primaryPlatform))
                    {
                        return false;
                    }

                    break;
                default:
                    Console.Error.WriteLine("Unknown option in <DefaultAction> tag of Module.xml.  Defaulting to resync!");
                    return this.ResyncProjectsForPlatform(
                        module,
                        primaryPlatform,
                        enabledServices,
                        disabledServices,
                        serviceSpecPath,
                        debugServiceResolution,
                        disablePackageResolution,
                        disableHostPlatformGeneration);
            }

            // Create the list of multiple platforms.
            var multiplePlatformsList =
                multiplePlatforms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            // If the host platform is not in the list, add it.  We do this because even when we are targeting
            // a mobile platform, we'll still want host projects like IDE editors and post-build hooks to
            // be built for the host platform so that we can develop for the mobile platform.
            var hostPlatformNormalized = module.NormalizePlatform(hostPlatform);
            if (hostPlatformNormalized == null)
            {
                Console.WriteLine(
                    "WARNING: The current host platform is not a supported platform for the solution.  IDE editor " +
                    "projects and post-build hooks will not be available, and this may cause the project to be " +
                    "built incorrectly!");
            }
            else if (!multiplePlatformsList.Contains(hostPlatformNormalized))
            {
                if (!disableHostPlatformGeneration)
                {
                    multiplePlatformsList.Add(hostPlatformNormalized);
                }
            }

            // Now iterate through the multiple platforms specified.
            foreach (var platformIter in multiplePlatformsList.Distinct())
            {
                if (platformIter == primaryPlatform)
                {
                    // Already handled above.
                    continue;
                }

                // Resolve submodules as needed.
                if (!disablePackageResolution)
                {
                    this.m_PackageManager.ResolveAll(module, platformIter);
                }

                switch (action.ToLower())
                {
                    case "generate":
                    case "resync":
                        // We do a generate under resync mode since we only want the primary platform
                        // to have synchronisation done (and it has had above).
                        if (!this.GenerateProjectsForPlatform(
                            module,
                            platformIter,
                            enabledServices,
                            disabledServices,
                            serviceSpecPath,
                            debugServiceResolution,
                            disablePackageResolution,
                            disableHostPlatformGeneration))
                        {
                            return false;
                        }

                        break;
                    case "clean":
                        if (!this.CleanProjectsForPlatform(module, platformIter))
                        {
                            return false;
                        }

                        break;
                    default:
                        throw new InvalidOperationException("Code should never reach this point");
                }
            }

            // All the steps succeeded, so return true.
            return true;
        }

        private void ShowSupportedPlatformsError(ModuleInfo module, string requestedPlatform)
        {
            Console.Error.WriteLine("The platform '" + requestedPlatform + "' is not supported.");
            Console.Error.WriteLine("The following platforms are supported by this module:");
            foreach (
                var supportedPlatform in
                module.SupportedPlatforms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                Console.Error.WriteLine("  * " + supportedPlatform);
            }

            ExecEnvironment.Exit(1);
        }

        /// <summary>
        /// Performs the default action on the specified module.
        /// </summary>
        /// <returns><c>true</c>, if the default action succeeded, <c>false</c> otherwise.</returns>
        /// <param name="module">The module to perform the action on.</param>
        /// <param name="platform">The platform to perform the action for.</param>
        /// <param name="enabledServices">A list of enabled services.</param>
        /// <param name="disabledServices">A list of disabled services.</param>
        /// <param name="serviceSpecPath">The service specification path.</param>
        /// <param name="debugServiceResolution">Whether to enable debugging information during service resolution.</param>
        /// <param name="disablePackageResolution">Whether to disable package resolution.</param>
        /// <param name="disableHostPlatformGeneration">Whether to disable generation of the host platform projects.</param>
        public bool DefaultAction(
            ModuleInfo module, 
            string platform = null, 
            string[] enabledServices = null, 
            string[] disabledServices = null, 
            string serviceSpecPath = null,
            bool debugServiceResolution = false,
            bool disablePackageResolution = false,
            bool disableHostPlatformGeneration = false)
        {
            return PerformAction(
                module, 
                module.DefaultAction,
                platform,
                enabledServices,
                disabledServices, 
                serviceSpecPath, 
                debugServiceResolution,
                disablePackageResolution,
                disableHostPlatformGeneration);
        }

        /// <summary>
        /// Resynchronises the projects for the specified platform.
        /// </summary>
        /// <returns><c>true</c>, if the resynchronisation succeeded, <c>false</c> otherwise.</returns>
        /// <param name="module">The module to resynchronise.</param>
        /// <param name="platform">The platform to resynchronise for.</param>
        /// <param name="enabledServices">A list of enabled services.</param>
        /// <param name="disabledServices">A list of disabled services.</param>
        /// <param name="serviceSpecPath">The service specification path.</param>
        /// <param name="debugServiceResolution">Whether to enable debugging information during service resolution.</param>
        /// <param name="disablePackageResolution">Whether to disable package resolution.</param>
        /// <param name="disableHostPlatformGeneration">Whether to disable generation of the host platform projects.</param>
        private bool ResyncProjectsForPlatform(
            ModuleInfo module, 
            string platform, 
            string[] enabledServices, 
            string[] disabledServices, 
            string serviceSpecPath,
            bool debugServiceResolution,
            bool disablePackageResolution,
            bool disableHostPlatformGeneration)
        {
            if (module.DisableSynchronisation ?? false)
            {
                Console.WriteLine("Synchronisation is disabled for " + module.Name + ".");
                Console.WriteLine("To generate projects, use the --generate option instead.");

                return false;
            }
            else
            {
                if (!SyncProjectsForPlatform(module, platform))
                {
                    return false;
                }

                return GenerateProjectsForPlatform(
                    module,
                    platform,
                    enabledServices,
                    disabledServices,
                    serviceSpecPath,
                    debugServiceResolution,
                    disablePackageResolution,
                    disableHostPlatformGeneration);
            }
        }

        /// <summary>
        /// Synchronises the projects for the specified platform.
        /// </summary>
        /// <returns><c>true</c>, if the synchronisation succeeded, <c>false</c> otherwise.</returns>
        /// <param name="module">The module to synchronise.</param>
        /// <param name="platform">The platform to synchronise for.</param>
        private bool SyncProjectsForPlatform(ModuleInfo module, string platform)
        {
            if (module.DisableSynchronisation ?? false)
            {
                Console.WriteLine("Synchronisation is disabled for " + module.Name + ".");
                return false;
            }

            if (string.IsNullOrWhiteSpace(platform))
            {
                platform = this.m_HostPlatformDetector.DetectPlatform();
            }

            var task = this.m_LightweightKernel.Get<SyncProjectsTask>();
            task.SourcePath = Path.Combine(module.Path, "Build", "Projects");
            task.RootPath = module.Path + Path.DirectorySeparatorChar;
            task.Platform = platform;
            task.ModuleName = module.Name;
            return task.Execute();
        }

        /// <summary>
        /// Generates the projects for the specified platform.
        /// </summary>
        /// <returns><c>true</c>, if the generation succeeded, <c>false</c> otherwise.</returns>
        /// <param name="module">The module whose projects should be generated.</param>
        /// <param name="platform">The platform to generate for.</param>
        /// <param name="enabledServices">A list of enabled services.</param>
        /// <param name="disabledServices">A list of disabled services.</param>
        /// <param name="serviceSpecPath">The service specification path.</param>
        /// <param name="debugServiceResolution">Whether to enable debugging information during service resolution.</param>
        /// <param name="disablePackageResolution">Whether to disable package resolution.</param>
        /// <param name="disableHostPlatformGeneration">Whether to disable generation of the host platform projects.</param>
        private bool GenerateProjectsForPlatform(
            ModuleInfo module,
            string platform,
            string[] enabledServices,
            string[] disabledServices,
            string serviceSpecPath,
            bool debugServiceResolution,
            bool disablePackageResolution,
            bool disableHostPlatformGeneration)
        {
            if (string.IsNullOrWhiteSpace(platform)) 
            {
                platform = this.m_HostPlatformDetector.DetectPlatform();
            }

            var task = this.m_LightweightKernel.Get<GenerateProjectsTask>();
            task.SourcePath = Path.Combine(module.Path, "Build", "Projects");
            task.RootPath = module.Path + Path.DirectorySeparatorChar;
            task.Platform = platform;
            task.ModuleName = module.Name;
            task.EnableServices = enabledServices;
            task.DisableServices = disabledServices;
            task.ServiceSpecPath = serviceSpecPath;
            task.DebugServiceResolution = debugServiceResolution;
            task.DisablePackageResolution = disablePackageResolution;
            task.DisableHostPlatformGeneration = disableHostPlatformGeneration;
            return task.Execute();
        }

        /// <summary>
        /// Cleans the projects for the specified platform.
        /// </summary>
        /// <returns><c>true</c>, if projects were cleaned successfully, <c>false</c> otherwise.</returns>
        /// <param name="module">The module to clean projects in.</param>
        /// <param name="platform">The platform to clean for.</param>
        private bool CleanProjectsForPlatform(ModuleInfo module, string platform)
        {
            if (string.IsNullOrWhiteSpace(platform))
            {
                platform = this.m_HostPlatformDetector.DetectPlatform();
            }

            var task = this.m_LightweightKernel.Get<CleanProjectsTask>();
            task.SourcePath = Path.Combine(module.Path, "Build", "Projects");
            task.RootPath = module.Path + Path.DirectorySeparatorChar;
            task.Platform = platform;
            task.ModuleName = module.Name;
            return task.Execute();
        }
    }
}

