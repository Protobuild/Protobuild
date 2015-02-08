namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageEverythingFailWhenBuildNotPresentTest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingFailWhenBuildNotPresent", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.lzma Windows Filter.txt --format tar/lzma", expectFailure: true, purge: false);
        }
    }
}