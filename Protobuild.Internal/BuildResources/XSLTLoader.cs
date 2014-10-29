using System;
using System.Xml.Xsl;
using System.Xml;
using System.IO;
using System.Reflection;

namespace Protobuild
{
    public static class XSLTLoader
    {
        public static XslCompiledTransform LoadGenerateProjectXSLT(string rootPath)
        {
            var resolver = new EmbeddedResourceResolver();
            var result = new XslCompiledTransform();
            using (var reader = XmlReader.Create(ResourceExtractor.GetGenerateProjectXSLT(rootPath)))
            {
                result.Load(
                    reader,
                    XsltSettings.TrustedXslt,
                    resolver
                );
            }
            return result;
        }

        public static XslCompiledTransform LoadNormalXSLT(string rootPath, string name)
        {
            var resolver = new EmbeddedResourceResolver();
            var result = new XslCompiledTransform();
            Stream generateSolutionStream;
            var generateSolutionXSLT = Path.Combine(rootPath, "Build", "GenerateSolution.xslt");
            if (File.Exists(generateSolutionXSLT))
                generateSolutionStream = File.Open(generateSolutionXSLT, FileMode.Open);
            else
                generateSolutionStream = ResourceExtractor.GetTransparentDecompressionStream(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(
                        name + ".xslt.lzma"));
            using (generateSolutionStream)
            {
                using (var reader = XmlReader.Create(generateSolutionStream))
                {
                    result.Load(
                        reader,
                        XsltSettings.TrustedXslt,
                        resolver
                    );
                }
            }
            return result;
        }
    }
}

