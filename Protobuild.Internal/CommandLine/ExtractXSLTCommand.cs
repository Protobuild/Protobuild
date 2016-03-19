using System;
using System.IO;
using System.Reflection;

namespace Protobuild
{
    internal class ExtractXSLTCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.CommandToExecute = this;
        }

        public int Execute(Execution execution)
        {
            if (Directory.Exists("Build"))
            {
                using (var writer = new StreamWriter(Path.Combine("Build", "GenerateProject.CSharp.xslt")))
                {
                    ResourceExtractor.GetTransparentDecompressionStream(
                        Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("GenerateProject.CSharp.xslt.lzma"))
                        .CopyTo(writer.BaseStream);
                    writer.Flush();
                }
                using (var writer = new StreamWriter(Path.Combine("Build", "GenerateProject.CPlusPlus.VisualStudio.xslt")))
                {
                    ResourceExtractor.GetTransparentDecompressionStream(
                        Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("GenerateProject.CPlusPlus.VisualStudio.xslt.lzma"))
                        .CopyTo(writer.BaseStream);
                    writer.Flush();
                }
                using (var writer = new StreamWriter(Path.Combine("Build", "GenerateProject.CPlusPlus.MonoDevelop.xslt")))
                {
                    ResourceExtractor.GetTransparentDecompressionStream(
                        Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("GenerateProject.CPlusPlus.MonoDevelop.xslt.lzma"))
                        .CopyTo(writer.BaseStream);
                    writer.Flush();
                }
                using (var writer = new StreamWriter(Path.Combine("Build", "GenerateSolution.xslt")))
                {
                    ResourceExtractor.GetTransparentDecompressionStream(
                        Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("GenerateSolution.xslt.lzma"))
                        .CopyTo(writer.BaseStream);
                    writer.Flush();
                }
                using (var writer = new StreamWriter(Path.Combine("Build", "GenerationFunctions.cs")))
                {
                    ResourceExtractor.GetTransparentDecompressionStream(
                        Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("GenerationFunctions.cs.lzma"))
                        .CopyTo(writer.BaseStream);
                    writer.Flush();
                }
                using (var writer = new StreamWriter(Path.Combine("Build", "SelectSolution.xslt")))
                {
                    ResourceExtractor.GetTransparentDecompressionStream(
                        Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("SelectSolution.xslt.lzma"))
                        .CopyTo(writer.BaseStream);
                    writer.Flush();
                }
            }

            return 0;
        }

        public string GetDescription()
        {
            return @"
Extracts the XSLT templates to the Build\ folder.  When present, these
are used over the built-in versions.  This allows you to customize
and extend the project / solution generation to support additional
features.
";
        }

        public int GetArgCount()
        {
            return 0;
        }

        public string[] GetArgNames()
        {
            return new string[0];
        }

        public bool IsInternal()
        {
            return false;
        }

        public bool IsRecognised()
        {
            return true;
        }

        public bool IsIgnored()
        {
            return false;
        }
    }
}

