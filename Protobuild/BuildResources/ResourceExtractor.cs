using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Protobuild
{
    public static class ResourceExtractor
    {
        public static void ExtractProject(string path, string projectName)
        {
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Protobuild.BuildResources.Main.proj"))
            {
                using (var writer = new StreamWriter(Path.Combine(path, "Main.proj")))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var text = reader.ReadToEnd();
                        text = text.Replace("{MODULE_NAME}", projectName);
                        writer.Write(text);
                        writer.Flush();
                    }
                }
            }
        }
    
        public static void ExtractNuGet(string path)
        {
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Protobuild.BuildResources.nuget.exe"))
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                using (var writer = new FileStream(Path.Combine(path, "nuget.exe"), FileMode.Create))
                {
                    writer.Write(bytes, 0, bytes.Length);
                    writer.Flush();
                }
                
                // Attempt to set the executable bit if possible.  On platforms
                // where this doesn't work, it doesn't matter anyway.
                try
                {
                    var p = Process.Start("chmod", "a+x " + Path.Combine(path, "nuget.exe").Replace("\\", "\\\\").Replace(" ", "\\ "));
                    p.WaitForExit();
                }
                catch (Exception)
                {
                }
            }
        }
        
        public static void ExtractUtilities(string path)
        {
            ExtractNuGet(path);
        }
        
        public static void ExtractAll(string path, string projectName)
        {
            ExtractProject(path, projectName);
            if (!Directory.Exists(Path.Combine(path, "Projects")))
                Directory.CreateDirectory(Path.Combine(path, "Projects"));
            var module = new ModuleInfo { Name = projectName };
            module.Save(Path.Combine(path, "Module.xml"));
        }
    }
}

