namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageEverythingFailWhenBuildModuleXmlNotPresentTest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingFailWhenBuildModuleXmlNotPresent", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.lzma Windows Filter.txt --format tar/lzma", expectFailure: true, purge: false);
        }
    }
}