namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageEverythingFailWhenProtobuildPresentTest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingFailWhenProtobuildPresent", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.lzma Windows Filter.txt --format tar/lzma", expectFailure: true, purge: false);
        }
    }
}