using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Xsl;
using System.Reflection;
using System.Xml;
using System.Text;

namespace Protobuild
{
    internal class ResourceProvider : IResourceProvider
    {
        private readonly ILanguageStringProvider m_LanguageStringProvider;

        private readonly IWorkingDirectoryProvider m_WorkingDirectoryProvider;

        private readonly IGenerationFunctionsProvider _generationFunctionsProvider;

        private static Dictionary<string, Dictionary<int, XslCompiledTransform>> m_CachedTransforms = new Dictionary<string, Dictionary<int, XslCompiledTransform>>();

        public ResourceProvider(
            ILanguageStringProvider languageStringProvider,
            IWorkingDirectoryProvider workingDirectoryProvider,
            IGenerationFunctionsProvider generationFunctionsProvider)
        {
            this.m_LanguageStringProvider = languageStringProvider;
            this.m_WorkingDirectoryProvider = workingDirectoryProvider;
            _generationFunctionsProvider = generationFunctionsProvider;
        }

        private class ReplacementInfo
        {
            public ResourceType ResourceName { get; set; }

            public Func<string, string> ReplacementProcessor { get; set; }
        }

        private string GetResourceExtension(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.GenerateProject:
                case ResourceType.GenerateSolution:
                case ResourceType.SelectSolution:
                case ResourceType.AdditionalProjectTransforms:
                    return "xslt";
                case ResourceType.GenerationFunctions:
                case ResourceType.AdditionalGenerationFunctions:
                    return "cs";
            }

            throw new InvalidOperationException();
        }

        private Stream LoadOverriddableResource(ResourceType resourceType, Language language, string platform)
        {
            string name = null;
            var extension = GetResourceExtension(resourceType);
            var fileSuffix = string.Empty;
            var replacements = new Dictionary<string, ReplacementInfo>();
            bool okayToFail = false;
            switch (resourceType)
            {
                case ResourceType.GenerateProject:
                    name = "GenerateProject";
                    fileSuffix = "." + this.m_LanguageStringProvider.GetFileSuffix(language);
                    replacements.Add("ADDITIONAL_TRANSFORMS", new ReplacementInfo
                    {
                        ResourceName = ResourceType.AdditionalProjectTransforms
                    });
                    break;
                case ResourceType.GenerateSolution:
                    name = "GenerateSolution";
                    break;
                case ResourceType.SelectSolution:
                    name = "SelectSolution";
                    break;
                case ResourceType.GenerationFunctions:
                    name = "GenerationFunctions";
                    break;
                case ResourceType.AdditionalGenerationFunctions:
                    name = "AdditionalGenerationFunctions";
                    okayToFail = true;
                    break;
                case ResourceType.AdditionalProjectTransforms:
                    name = "AdditionalProjectTransforms";
                    okayToFail = true;
                    break;
                default:
                    throw new NotSupportedException();
            }

            switch (resourceType)
            {
                case ResourceType.GenerateProject:
                case ResourceType.GenerateSolution:
                case ResourceType.SelectSolution:
                    replacements.Add("GENERATION_FUNCTIONS", new ReplacementInfo
                    {
                        ResourceName = ResourceType.GenerationFunctions,
                        ReplacementProcessor = x => _generationFunctionsProvider.ConvertGenerationFunctionsToXSLT("user", x)
                    });
                    replacements.Add("ADDITIONAL_GENERATION_FUNCTIONS", new ReplacementInfo
                    {
                        ResourceName = ResourceType.AdditionalGenerationFunctions,
                        ReplacementProcessor = x => _generationFunctionsProvider.ConvertGenerationFunctionsToXSLT("extra", x)
                    });
                    break;
            }

            var onDiskNames = new List<string>();

            onDiskNames.Add(name + fileSuffix + "." + platform + "." + extension);
            onDiskNames.Add(name + fileSuffix + "." + extension);

            if (resourceType == ResourceType.GenerateProject && language == Language.CSharp)
            {
                onDiskNames.Add(name + "." + extension);
            }

            Stream source = null;

            var globalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".protobuild-xslt");
            if (Directory.Exists(globalPath))
            {
                foreach (var filename in onDiskNames)
                {
                    var path = Path.Combine(globalPath, filename);

                    if (File.Exists(path))
                    {
                        source = File.Open(path, FileMode.Open);
                        break;
                    }
                }
            }

            if (source == null)
            {
                foreach (var filename in onDiskNames)
                {
                    var path = Path.Combine(this.m_WorkingDirectoryProvider.GetPath(), "Build", filename);

                    if (File.Exists(path))
                    {
                        source = File.Open(path, FileMode.Open);
                        break;
                    }
                }
            }

            if (source == null)
            {
                var embeddedName = name + fileSuffix + "." + extension + ".lzma";
                var embeddedStream = Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream(embeddedName);
                if (embeddedStream == null)
                {
                    if (okayToFail)
                    {
                        return new MemoryStream();
                    }
                    else
                    {
                        throw new InvalidOperationException("No embedded stream with name '" + embeddedName + "'");
                    }
                }
                source = this.GetTransparentDecompressionStream(embeddedStream);
            }

            foreach (var replacement in replacements)
            {
                var memory = new MemoryStream();
                using (var stream = source)
                {
                    using (var writer = new StringWriter())
                    {
                        var replacementDataStream = this.LoadOverriddableResource(
                            replacement.Value.ResourceName,
                            language,
                            platform);
                        string replacementData;
                        using (var reader = new StreamReader(replacementDataStream))
                        {
                            replacementData = reader.ReadToEnd();
                        }

                        if (replacement.Value.ReplacementProcessor != null)
                        {
                            replacementData = replacement.Value.ReplacementProcessor(replacementData);
                        }

                        using (var reader = new StreamReader(stream))
                        {
                            var text = reader.ReadToEnd();
                            text = text.Replace("<!-- {" + replacement.Key + "} -->", replacementData);
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

            return source;
        }

        public XslCompiledTransform LoadXSLT(ResourceType resourceType, Language language, string platform)
        {
            var currentDir = m_WorkingDirectoryProvider.GetPath();
            if (!m_CachedTransforms.ContainsKey(currentDir))
            {
                m_CachedTransforms[currentDir] = new Dictionary<int, XslCompiledTransform>();
            }
            var cache = m_CachedTransforms[currentDir];

            int hash;
            unchecked
            {
                hash = 17 *
                       resourceType.GetHashCode() * 31 +
                       language.GetHashCode() * 31 +
                       platform.GetHashCode() * 31;
            }
            if (cache.ContainsKey(hash))
            {
                return cache[hash];
            }

            var source = this.LoadOverriddableResource(resourceType, language, platform);

            var reader2 = new StreamReader(source);
            var readerInspect = reader2.ReadToEnd();
            source.Seek(0, SeekOrigin.Begin);

            var resolver = new EmbeddedResourceResolver();
            var result = new XslCompiledTransform();
            using (var reader = XmlReader.Create(source))
            {
                try
                {
                    result.Load(
                        reader,
                        XsltSettings.TrustedXslt,
                        resolver
                        );
                }
                catch (XsltException ex)
                {
                    var lines = readerInspect.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
                    var line = ex.LineNumber;
                    var minLine = Math.Max(line - 10, 0);
                    var maxLine = Math.Min(line + 10, lines.Length - 1);
                    var selectedLines = new List<string>();
                    for (var l = minLine; l <= maxLine; l++)
                    {
                        if (l == line)
                        {
                            selectedLines.Add(">> " + lines[l]);
                        }
                        else
                        {
                            selectedLines.Add("   " + lines[l]);
                        }
                    }
                    var context = string.Join(Environment.NewLine, selectedLines);
                    throw new XsltCompileException(
                        ex.Message + "\r\n" + context,
                        ex);
                }
            }

            cache[hash] = result;
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

