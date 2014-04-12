using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Protobuild
{
    using System.IO.Compression;

    public static class ResourceExtractor
    {
        public static Stream GetTransparentDecompressionStream(Stream input)
        {
            return new GZipStream(input, CompressionMode.Decompress);
        }

        public static StringReader GetGenerateProjectXSLT(string path)
        {
            Stream generateProjectStream;
            var generateProjectXSLT = Path.Combine(path, "Build", "GenerateProject.xslt");
            if (File.Exists(generateProjectXSLT))
                generateProjectStream = File.Open(generateProjectXSLT, FileMode.Open);
            else
                generateProjectStream = GetTransparentDecompressionStream(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "Protobuild.BuildResources.GenerateProject.xslt.gz"));
            
            using (var stream = generateProjectStream)
            {
                using (var writer = new StringWriter())
                {
                    var additional = "";
                    var additionalPath = Path.Combine(path, "Build", "AdditionalProjectTransforms.xslt");
                    if (File.Exists(additionalPath))
                    {
                        using (var reader = new StreamReader(additionalPath))
                        {
                            additional = reader.ReadToEnd();
                        }
                    }
                    using (var reader = new StreamReader(stream))
                    {
                        var text = reader.ReadToEnd();
                        text = text.Replace("{ADDITIONAL_TRANSFORMS}", additional);
                        writer.Write(text);
                        writer.Flush();
                    }
                    return new StringReader(writer.GetStringBuilder().ToString());
                }
            }
        }
        
        public static void ExtractAll(string path, string projectName)
        {
            if (!Directory.Exists(Path.Combine(path, "Projects")))
                Directory.CreateDirectory(Path.Combine(path, "Projects"));
            var module = new ModuleInfo { Name = projectName };
            module.Save(Path.Combine(path, "Module.xml"));
        }

        public static void ExtractJSILTemplate(string name, string targetPath)
        {
            Stream jsilTemplateStream = GetTransparentDecompressionStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "Protobuild.BuildResources.JSILTemplate.htm.gz"));

            using (var stream = jsilTemplateStream)
            {
                using (var writer = new StringWriter())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var text = reader.ReadToEnd();
                        text = text.Replace("{NAME}", name);
                        writer.Write(text);
                        writer.Flush();
                    }

                    var content = writer.GetStringBuilder().ToString();

                    using (var fileWriter = new StreamWriter(targetPath))
                    {
                        fileWriter.Write(content);
                    }
                }
            }
        }
    }
}

