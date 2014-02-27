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
        
        public static bool ResyncProjects(ModuleInfo module, string platform = null)
        {
            if (!SyncProjects(module, platform))
                return false;
            return GenerateProjects(module, platform);
        }
        
        public static bool SyncProjects(ModuleInfo module, string platform = null)
        {
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

        public static bool GenerateProjects(ModuleInfo module, string platform = null)
        {
            if (string.IsNullOrWhiteSpace(platform))
                platform = DetectPlatform();
                
            var task = new GenerateProjectsTask
            {
                SourcePath = Path.Combine(module.Path, "Build", "Projects"),
                RootPath = module.Path + Path.DirectorySeparatorChar,
                Platform = platform,
                ModuleName = module.Name
            };
            return task.Execute();
        }
        
        public static bool CleanProjects(ModuleInfo module, string platform = null)
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
        
        private static string DetectPlatform()
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                if (Directory.Exists("/Library"))
                    return "MacOS";
                return "Linux";
            }
            return "Windows";
        }

        public static bool DefaultAction(ModuleInfo module)
        {
            var platform = DetectPlatform();
            var primaryPlatform = platform;
            
            // You can generate multiple targets by default by setting the <DefaultWindowsPlatforms>
            // <DefaultMacOSPlatforms> and <DefaultLinuxPlatforms> tags in Module.xml.  Note that
            // synchronisation will only be done for the primary platform, as there is no correct
            // synchronisation behaviour when dealing with multiple C# projects.
            string multiplePlatforms = null;
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
            switch (module.DefaultAction.ToLower())
            {
                case "generate":
                    break;
                case "resync":
                    if (!Actions.ResyncProjects(module, primaryPlatform))
                    {
                        return false;
                    }

                    break;
                case "sync":
                    return Actions.SyncProjects(module, primaryPlatform);
                default:
                    Console.Error.WriteLine("Unknown option in <DefaultAction> tag of Module.xml.  Defaulting to resync!");
                    return Actions.ResyncProjects(module, primaryPlatform);
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

                switch (module.DefaultAction.ToLower())
                {
                    case "generate":
                    case "resync":
                        // We do a generate under resync mode since we only want the primary platform
                        // to have synchronisation done (and it has had above).
                        if (!Actions.GenerateProjects(module, platformIter))
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
    }
}

