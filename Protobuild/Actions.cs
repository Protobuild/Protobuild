using System;
using System.Diagnostics;
using System.IO;
using Protobuild.Tasks;

namespace Protobuild
{
    using System.Linq;

    public static class Actions
    {
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

        public static bool ResyncProjectsForPlatform(ModuleInfo module, string platform, string[] enabledServices = null, string[] disabledServices = null, string serviceSpecPath = null)
        {
            if (module.DisableSynchronisation ?? false)
            {
                Console.WriteLine("Synchronisation is disabled for " + module.Name + ".");
            }
            else
            {
                if (!SyncProjectsForPlatform(module, platform))
                    return false;
            }

            return GenerateProjectsForPlatform(module, platform, enabledServices, disabledServices, serviceSpecPath);
        }
        
        public static bool SyncProjectsForPlatform(ModuleInfo module, string platform)
        {
            if (module.DisableSynchronisation ?? false)
            {
                Console.WriteLine("Synchronisation is disabled for " + module.Name + ".");
                return false;
            }

            if (string.IsNullOrWhiteSpace(platform))
                platform = DetectPlatform();
                
            var task = new SyncProjectsTask
            {
                SourcePath = Path.Combine(module.Path, "Build", "Projects"),
                RootPath = module.Path + Path.DirectorySeparatorChar,
                Platform = platform,
                ModuleName = module.Name
            };
            return task.Execute();
        }

        public static bool GenerateProjectsForPlatform(
            ModuleInfo module,
            string platform,
            string[] enabledServices = null,
            string[] disabledServices = null,
            string serviceSpecPath = null)
        {
            if (string.IsNullOrWhiteSpace(platform)) platform = DetectPlatform();

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

        public static bool CleanProjectsForPlatform(ModuleInfo module, string platform)
        {
            if (string.IsNullOrWhiteSpace(platform))
                platform = DetectPlatform();
                
            var task = new CleanProjectsTask
            {
                SourcePath = Path.Combine(module.Path, "Build", "Projects"),
                RootPath = module.Path + Path.DirectorySeparatorChar,
                Platform = platform,
                ModuleName = module.Name
            };
            return task.Execute();
        }
        
        public static string DetectPlatform()
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                if (Directory.Exists("/Library"))
                    return "MacOS";
                return "Linux";
            }
            return "Windows";
        }

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

        public static bool DefaultAction(ModuleInfo module, string platform = null, string[] enabledServices = null, string[] disabledServices = null, string serviceSpecPath = null)
        {
            return PerformAction(module, module.DefaultAction, platform, enabledServices, disabledServices, serviceSpecPath);
        }
    }
}

