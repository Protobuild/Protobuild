//-----------------------------------------------------------------------
// <copyright file="Actions.cs" company="Protobuild Project">
// The MIT License (MIT)
// 
// Copyright (c) Various Authors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//     The above copyright notice and this permission notice shall be included in
//     all copies or substantial portions of the Software.
// 
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//     THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Protobuild
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Protobuild.Tasks;

    /// <summary>
    /// Provides utility methods for performing general actions in Protobuild.
    /// </summary>
    public static class Actions
    {
        /// <summary>
        /// Opens the specified object in MonoDevelop (to edit the definition file).
        /// </summary>
        /// <param name="root">The module that contains the object.</param>
        /// <param name="obj">The object to edit (either DefinitionInfo or ModuleInfo).</param>
        /// <param name="update">The callback when the module has been updated.</param>
        public static void Open(ModuleInfo root, object obj, Action update)
        {
            var definitionInfo = obj as DefinitionInfo;
            var moduleInfo = obj as ModuleInfo;
            if (definitionInfo != null)
            {
                // Open XML in editor.
                Process.Start("monodevelop", definitionInfo.DefinitionPath);
            }

            if (moduleInfo != null)
            {
                // Start the module's Protobuild unless it's also our
                // module (for the root node).
                if (moduleInfo.Path != root.Path)
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = System.IO.Path.Combine(moduleInfo.Path, "Protobuild.exe"),
                        WorkingDirectory = moduleInfo.Path
                    };
                    var p = Process.Start(info);
                    p.EnableRaisingEvents = true;
                    p.Exited += (object sender, EventArgs e) => update();
                }
            }
        }

        /// <summary>
        /// Detects the current executing (host) platform.
        /// </summary>
        /// <returns>The executing, host platform.</returns>
        public static string DetectPlatform()
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                if (Directory.Exists("/Library"))
                {
                    return "MacOS";
                }

                return "Linux";
            }

            return "Windows";
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
        public static bool PerformAction(ModuleInfo module, string action, string platform = null, string[] enabledServices = null, string[] disabledServices = null, string serviceSpecPath = null)
        {
            var platformSupplied = !string.IsNullOrWhiteSpace(platform);

            if (string.IsNullOrWhiteSpace(platform))
            {
                platform = DetectPlatform();
            }

            var originalPlatform = platform;
            platform = module.NormalizePlatform(platform);

            if (platform == null && !platformSupplied)
            {
                // The current host platform isn't supported, so we shouldn't try to
                // operate on it.
                string firstPlatform = null;
                switch (DetectPlatform())
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
                Console.Error.WriteLine("The platform '" + originalPlatform + "' is not supported.");
                Console.Error.WriteLine("The following platforms are supported by this module:");
                foreach (
                    var supportedPlatform in
                        module.SupportedPlatforms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    Console.Error.WriteLine("  * " + supportedPlatform);
                }

                Environment.Exit(1);
                return false;
            }

            var primaryPlatform = platform;
            
            // You can generate multiple targets by default by setting the <DefaultWindowsPlatforms>
            // <DefaultMacOSPlatforms> and <DefaultLinuxPlatforms> tags in Module.xml.  Note that
            // synchronisation will only be done for the primary platform, as there is no correct
            // synchronisation behaviour when dealing with multiple C# projects.
            //
            // We only trigger this behaviour when the platform is omitted; if you explicitly
            // specify "Windows" on the command line, we'll only generate / resync / sync
            // the Windows platform.
            string multiplePlatforms = null;
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

            // You can configure the default action for Protobuild in their project
            // with the <DefaultAction> tag in Module.xml.  If omitted, default to a resync.
            // Valid options for this tag are either "Generate", "Resync" or "Sync".

            // If the actions are "Resync" or "Sync", then we need to perform an initial
            // step against the primary platform.
            switch (action.ToLower())
            {
                case "generate":
                    if (!Actions.GenerateProjectsForPlatform(module, primaryPlatform, enabledServices, disabledServices, serviceSpecPath))
                    {
                        return false;
                    }

                    break;
                case "resync":
                    if (!Actions.ResyncProjectsForPlatform(module, primaryPlatform, enabledServices, disabledServices, serviceSpecPath))
                    {
                        return false;
                    }

                    break;
                case "sync":
                    return Actions.SyncProjectsForPlatform(module, primaryPlatform);
                case "clean":
                    if (!Actions.CleanProjectsForPlatform(module, primaryPlatform))
                    {
                        return false;
                    }

                    break;
                default:
                    Console.Error.WriteLine("Unknown option in <DefaultAction> tag of Module.xml.  Defaulting to resync!");
                    return Actions.ResyncProjectsForPlatform(module, primaryPlatform, enabledServices, disabledServices, serviceSpecPath);
            }

            // Now iterate through the multiple platforms specified.
            var multiplePlatformsArray =
                multiplePlatforms.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            foreach (var platformIter in multiplePlatformsArray)
            {
                if (platformIter == primaryPlatform)
                {
                    // Already handled above.
                    continue;
                }

                switch (action.ToLower())
                {
                    case "generate":
                    case "resync":
                        // We do a generate under resync mode since we only want the primary platform
                        // to have synchronisation done (and it has had above).
                        if (!Actions.GenerateProjectsForPlatform(module, platformIter, enabledServices, disabledServices, serviceSpecPath))
                        {
                            return false;
                        }

                        break;
                    case "clean":
                        if (!Actions.CleanProjectsForPlatform(module, platformIter))
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

        /// <summary>
        /// Performs the default action on the specified module.
        /// </summary>
        /// <returns><c>true</c>, if the default action succeeded, <c>false</c> otherwise.</returns>
        /// <param name="module">The module to perform the action on.</param>
        /// <param name="platform">The platform to perform the action for.</param>
        /// <param name="enabledServices">A list of enabled services.</param>
        /// <param name="disabledServices">A list of disabled services.</param>
        /// <param name="serviceSpecPath">The service specification path.</param>
        public static bool DefaultAction(ModuleInfo module, string platform = null, string[] enabledServices = null, string[] disabledServices = null, string serviceSpecPath = null)
        {
            return PerformAction(module, module.DefaultAction, platform, enabledServices, disabledServices, serviceSpecPath);
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
        private static bool ResyncProjectsForPlatform(ModuleInfo module, string platform, string[] enabledServices = null, string[] disabledServices = null, string serviceSpecPath = null)
        {
            if (module.DisableSynchronisation ?? false)
            {
                Console.WriteLine("Synchronisation is disabled for " + module.Name + ".");
            }
            else
            {
                if (!SyncProjectsForPlatform(module, platform))
                {
                    return false;
                }
            }

            return GenerateProjectsForPlatform(module, platform, enabledServices, disabledServices, serviceSpecPath);
        }

        /// <summary>
        /// Synchronises the projects for the specified platform.
        /// </summary>
        /// <returns><c>true</c>, if the synchronisation succeeded, <c>false</c> otherwise.</returns>
        /// <param name="module">The module to synchronise.</param>
        /// <param name="platform">The platform to synchronise for.</param>
        private static bool SyncProjectsForPlatform(ModuleInfo module, string platform)
        {
            if (module.DisableSynchronisation ?? false)
            {
                Console.WriteLine("Synchronisation is disabled for " + module.Name + ".");
                return false;
            }

            if (string.IsNullOrWhiteSpace(platform))
            {
                platform = DetectPlatform();
            }

            var task = new SyncProjectsTask
            {
                SourcePath = Path.Combine(module.Path, "Build", "Projects"),
                RootPath = module.Path + Path.DirectorySeparatorChar,
                Platform = platform,
                ModuleName = module.Name
            };
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
        private static bool GenerateProjectsForPlatform(
            ModuleInfo module,
            string platform,
            string[] enabledServices = null,
            string[] disabledServices = null,
            string serviceSpecPath = null)
        {
            if (string.IsNullOrWhiteSpace(platform)) 
            {
                platform = DetectPlatform();
            }

            var task = new GenerateProjectsTask
            {
                SourcePath = Path.Combine(module.Path, "Build", "Projects"),
                RootPath = module.Path + Path.DirectorySeparatorChar,
                Platform = platform,
                ModuleName = module.Name,
                EnableServices = enabledServices,
                DisableServices = disabledServices,
                ServiceSpecPath = serviceSpecPath
            };
            return task.Execute();
        }

        /// <summary>
        /// Cleans the projects for the specified platform.
        /// </summary>
        /// <returns><c>true</c>, if projects were cleaned successfully, <c>false</c> otherwise.</returns>
        /// <param name="module">The module to clean projects in.</param>
        /// <param name="platform">The platform to clean for.</param>
        private static bool CleanProjectsForPlatform(ModuleInfo module, string platform)
        {
            if (string.IsNullOrWhiteSpace(platform))
            {
                platform = DetectPlatform();
            }

            var task = new CleanProjectsTask
            {
                SourcePath = Path.Combine(module.Path, "Build", "Projects"),
                RootPath = module.Path + Path.DirectorySeparatorChar,
                Platform = platform,
                ModuleName = module.Name
            };
            return task.Execute();
        }
    }
}
