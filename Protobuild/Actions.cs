using System;
using System.Diagnostics;
using System.IO;

namespace Protobuild
{
    public static class Actions
    {
        private static string m_CachedToolName = null;

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
        
        public static void Sync(ModuleInfo module, string platform = null)
        {
            if (string.IsNullOrWhiteSpace(platform))
                platform = DetectPlatform();
            var definitions = module.GetDefinitions();
            foreach (var def in definitions)
            {
                // Read the project file in.
                var path = Path.Combine(module.Path, def.Name, def.Name + "." + platform + ".csproj");
                if (File.Exists(path))
                {
                    var project = CSharpProject.Load(path);
                    var synchroniser = new DefinitionSynchroniser(def, project);
                    synchroniser.Synchronise();
                }
            }
        }
    
        public static void Resync(ModuleInfo module, string platform = null)
        {
            Sync(module, platform);
            RegenerateProjects(module.Path, platform);
        }

        private static void TrySetTool(string tool, bool shellExecute = false)
        {
            if (m_CachedToolName != null)
                return;
            try
            {
                if (shellExecute)
                    Process.Start(tool, "/?");
                else
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = tool,
                        Arguments = "/?",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    Process.Start(info);
                    m_CachedToolName = tool;
                }
            }
            catch
            {
            }
        }

        private static string GetBuildToolName()
        {
            if (m_CachedToolName != null)
                return m_CachedToolName;
            TrySetTool("/usr/bin/xbuild");
            TrySetTool(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe");
            TrySetTool(@"C:\Windows\Microsoft.NET\Framework64\v3.5\msbuild.exe");
            TrySetTool(@"C:\Windows\Microsoft.NET\Framework64\v3.0\msbuild.exe");
            TrySetTool(@"C:\Windows\Microsoft.NET\Framework64\v2.0.50727\msbuild.exe");
            TrySetTool("xbuild", true);
            TrySetTool("msbuild", true);
            if (m_CachedToolName == null)
                throw new InvalidOperationException("Neither xbuild nor msbuild is in the PATH.");
            return m_CachedToolName;
        }

        public static int RegenerateProjects(string root, string platform = null)
        {
            if (string.IsNullOrWhiteSpace(platform))
                platform = DetectPlatform();
            var info = new ProcessStartInfo
            {
                FileName = GetBuildToolName(),
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
                FileName = GetBuildToolName(),
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

