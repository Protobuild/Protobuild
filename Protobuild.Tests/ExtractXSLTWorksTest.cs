namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ExtractXSLTWorksTest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("ExtractXSLTWorks", isPackTest: true);

            if (File.Exists(this.GetPath("Build\\GenerateProject.xslt")))
            {
                File.Delete(this.GetPath("Build\\GenerateProject.xslt"));
            }

            if (File.Exists(this.GetPath("Build\\SelectSolution.xslt")))
            {
                File.Delete(this.GetPath("Build\\SelectSolution.xslt"));
            }

            if (File.Exists(this.GetPath("Build\\GenerateSolution.xslt")))
            {
                File.Delete(this.GetPath("Build\\GenerateSolution.xslt"));
            }

            this.OtherMode("extract-xslt");
        }
    }
}