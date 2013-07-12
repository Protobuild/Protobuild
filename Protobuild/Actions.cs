using System;
using System.Diagnostics;
using System.IO;

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
        
        public static void Sync(ModuleInfo module)
        {
            var definitions = module.GetDefinitions();
            foreach (var def in definitions)
            {
                // Read the project file in.
                var path = Path.Combine(module.Path, def.Name, def.Name + "." + DetectPlatform() + ".csproj");
                if (File.Exists(path))
                {
                    var project = CSharpProject.Load(path);
                    var synchroniser = new DefinitionSynchroniser(def, project);
                    synchroniser.Synchronise();
                }
            }
        }
    
        public static void Resync(ModuleInfo module)
        {
            Sync(module);
            RegenerateProjects(module.Path);
        }
        
        public static int RegenerateProjects(string root, string platform = null)
        {
            if (string.IsNullOrWhiteSpace(platform))
                platform = DetectPlatform();
            var info = new ProcessStartInfo
            {
                FileName = "xbuild",
                Arguments = "Build" + Path.DirectorySeparatorChar + "Main.proj /p:TargetPlatform=" + platform,
                WorkingDirectory = root
            };
            var p = Process.Start(info);
            p.WaitForExit();
            return p.ExitCode;
        }
        
        public static int CleanProjects(string root, string platform = null)
        {
            if (string.IsNullOrWhiteSpace(platform))
                platform = DetectPlatform();
            var info = new ProcessStartInfo
            {
                FileName = "xbuild",
                Arguments = "Build" + Path.DirectorySeparatorChar + "Main.proj /p:TargetPlatform=" + platform + " /p:Clean=True",
                WorkingDirectory = root
            };
            var p = Process.Start(info);
            p.WaitForExit();
            return p.ExitCode;
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

