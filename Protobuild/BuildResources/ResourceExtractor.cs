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
        
        public static StringReader GetGenerateProjectXSLT(string path)
        {
            Stream generateProjectStream;
            var generateProjectXSLT = Path.Combine(path, "Build", "GenerateProject.xslt");
            if (File.Exists(generateProjectXSLT))
                generateProjectStream = File.Open(generateProjectXSLT, FileMode.Open);
            else
                generateProjectStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "Protobuild.BuildResources.GenerateProject.xslt");
            
            using (var stream = generateProjectStream)
            {
                using (var writer = new StringWriter())
                {
                    var additional = "";
                    string replacementDefinitions;
                    var additionalPath = Path.Combine(path, "Build", "AdditionalProjectTransforms.xslt");
                    var replacementDefinitionsPath = Path.Combine(path, "Build", "ReplacementDefinitions.xslt");
                    if (File.Exists(additionalPath))
                    {
                        using (var reader = new StreamReader(additionalPath))
                        {
                            additional = reader.ReadToEnd();
                        }
                    }
                    if (File.Exists(replacementDefinitionsPath))
                    {
                        using (var reader = new StreamReader(replacementDefinitionsPath))
                        {
                            replacementDefinitions = reader.ReadToEnd();
                        }
                    }
                    else
                    {
                        using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(
                            "Protobuild.BuildResources.ReplacementDefinitions.xslt")))
                        {
                            replacementDefinitions = reader.ReadToEnd();
                        }
                    }
                    using (var reader = new StreamReader(stream))
                    {
                        var text = reader.ReadToEnd();
                        text = text.Replace("{ADDITIONAL_TRANSFORMS}", additional);
                        text = text.Replace("{REPLACEMENT_DEFINITIONS}", replacementDefinitions);
                        writer.Write(text);
                        writer.Flush();
                    }
                    return new StringReader(writer.GetStringBuilder().ToString());
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
        
        public static void ExtractDefinitions(string path)
        {
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Protobuild.BuildResources.ReplacementDefinitions.xslt"))
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                using (var writer = new FileStream(Path.Combine(path, "ReplacementDefinitions.xslt"), FileMode.Create))
                {
                    writer.Write(bytes, 0, bytes.Length);
                    writer.Flush();
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

