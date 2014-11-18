namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageEverythingCorrectGZipTest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingCorrectGZip", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.gz Windows Filter.txt --format tar/gzip", purge: false);

            var packagedFiles = this.LoadPackage("Test.tar.gz");

            Assert.Contains("Build/", packagedFiles.Keys);
            Assert.Contains("Build/Module.xml", packagedFiles.Keys);
            Assert.Contains("Build/Projects/", packagedFiles.Keys);
            Assert.Contains("Build/Projects/Console.definition", packagedFiles.Keys);
            Assert.Contains("Console/", packagedFiles.Keys);
            Assert.Contains("Console/Program.cs", packagedFiles.Keys);
            Assert.Equal(6, packagedFiles.Count);
        }
    }
}