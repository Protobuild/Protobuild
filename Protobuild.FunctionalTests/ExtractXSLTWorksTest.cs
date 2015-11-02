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

            if (File.Exists(this.GetPath("Build\\GenerateProject.CSharp.xslt")))
            {
                File.Delete(this.GetPath("Build\\GenerateProject.CSharp.xslt"));
            }

            if (File.Exists(this.GetPath("Build\\SelectSolution.MSBuild.xslt")))
            {
                File.Delete(this.GetPath("Build\\SelectSolution.MSBuild.xslt"));
            }

            if (File.Exists(this.GetPath("Build\\GenerateSolution.MSBuild.xslt")))
            {
                File.Delete(this.GetPath("Build\\GenerateSolution.MSBuild.xslt"));
            }

            this.OtherMode("extract-xslt");
        }
    }
}