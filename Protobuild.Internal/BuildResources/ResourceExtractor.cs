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
            var memory = new MemoryStream();
            LZMA.LzmaHelper.Decompress(input, memory);
            memory.Seek(0, SeekOrigin.Begin);
            return memory;
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
                "JSILTemplate.htm.lzma"));

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

