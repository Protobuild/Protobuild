namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExtractXSLTWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExtractXSLTWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void PackageIsCorrect()
        {
            this.SetupTest("ExtractXSLTWorks", isPackTest: true);

            var files = new[]
            {
                "GenerateProject.CSharp.xslt",
                "GenerateProject.CPlusPlus.MonoDevelop.xslt",
                "GenerateProject.CPlusPlus.VisualStudio.xslt",
                "GenerateSolution.xslt",
                "GenerationFunctions.cs",
                "SelectSolution.xslt",
            };

            foreach (var file in files)
            {
                if (File.Exists(this.GetPath("Build\\" + file)))
                {
                    File.Delete(this.GetPath("Build\\" + file));
                }
            }

            this.OtherMode("extract-xslt");

            foreach (var file in files)
            {
                _assert.True(File.Exists(this.GetPath("Build\\" + file)));
            }
        }
    }
}