namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageEverythingFailWhenBuildProjectsNotPresentTest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingFailWhenBuildProjectsNotPresent", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.lzma Windows Filter.txt --format tar/lzma", expectFailure: true, purge: false);
        }
    }
}