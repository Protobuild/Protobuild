using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Xsl;
using System.Reflection;
using System.Xml;
using System.Text;

namespace Protobuild
{
    public class ResourceProvider : IResourceProvider
    {
        private readonly ILanguageStringProvider m_LanguageStringProvider;

        private readonly IWorkingDirectoryProvider m_WorkingDirectoryProvider;

        public ResourceProvider(
            ILanguageStringProvider languageStringProvider,
            IWorkingDirectoryProvider workingDirectoryProvider)
        {
            this.m_LanguageStringProvider = languageStringProvider;
            this.m_WorkingDirectoryProvider = workingDirectoryProvider;
        }

        public XslCompiledTransform LoadXSLT(ResourceType resourceType, Language language)
        {
            string name = null;
            string fileSuffix = string.Empty;
            bool applyAdditionalTransforms = false;
            switch (resourceType)
            {
                case ResourceType.GenerateProject:
                    name = "GenerateProject";
                    fileSuffix = "." + this.m_LanguageStringProvider.GetFileSuffix(language);
                    applyAdditionalTransforms = true;
                    break;
                case ResourceType.GenerateSolution:
                    name = "GenerateSolution";
                    break;
                case ResourceType.SelectSolution:
                    name = "SelectSolution";
                    break;
                default:
                    throw new NotSupportedException();
            }

            var onDiskNames = new List<string>();
            onDiskNames.Add(name + fileSuffix + ".xslt");

            if (resourceType == ResourceType.GenerateProject && language == Language.CSharp)
            {
                onDiskNames.Add(name + ".xslt");
            }

            Stream source = null;

            foreach (var filename in onDiskNames)
            {
                var path = Path.Combine(this.m_WorkingDirectoryProvider.GetPath(), "Build", filename);

                if (File.Exists(path))
                {
                    source = File.Open(path, FileMode.Open);
                    break;
                }
            }

            if (source == null)
            {
                var embeddedName = name + fileSuffix + ".xslt.lzma";
                var embeddedStream = Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream(embeddedName);
                source = this.GetTransparentDecompressionStream(embeddedStream);
            }

            if (applyAdditionalTransforms)
            {
                var memory = new MemoryStream();
                using (var stream = source)
                {
                    using (var writer = new StringWriter())
                    {
                        var additional = "";
                        var additionalPath = Path.Combine(this.m_WorkingDirectoryProvider.GetPath(), "Build", "AdditionalProjectTransforms.xslt");
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

                        var resultBytes = Encoding.UTF8.GetBytes(writer.GetStringBuilder().ToString());
                        memory.Write(resultBytes, 0, resultBytes.Length);
                        memory.Seek(0, SeekOrigin.Begin);
                    }
                }

                source = memory;
            }

            var resolver = new EmbeddedResourceResolver();
            var result = new XslCompiledTransform();
            using (var reader = XmlReader.Create(source))
            {
                result.Load(
                    reader,
                    XsltSettings.TrustedXslt,
                    resolver
                );
            }
            return result;
        }

        private Stream GetTransparentDecompressionStream(Stream input)
        {
            var memory = new MemoryStream();
            LZMA.LzmaHelper.Decompress(input, memory);
            memory.Seek(0, SeekOrigin.Begin);
            return memory;
        }
    }
}

