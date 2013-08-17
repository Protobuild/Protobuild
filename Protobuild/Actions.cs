using System;
using System.Diagnostics;
using System.IO;
using Protobuild.Tasks;

namespace Protobuild
{
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
                SourcePath = @"..\Build\Projects",
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
                SourcePath = @"..\Build\Projects",
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
                SourcePath = @"..\Build\Projects",
                RootPath = module.Path + Path.DirectorySeparatorChar,
                Platform = platform,
                ModuleName = module.Name
            };
            return task.Execute();
        }
        
        private static string DetectPlatform()
        {
            if (System.IO.Path.DirectorySeparatorChar == '/')
            {
                if (Directory.Exists("/home"))
                    return "Linux";
                return "MacOS";
            }
            return "Windows";
        }
        
    }
}

