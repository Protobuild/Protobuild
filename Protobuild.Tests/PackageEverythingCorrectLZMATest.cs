namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageEverythingCorrectLZMATest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingCorrectLZMA", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.lzma Windows Filter.txt --format tar/lzma", purge: false);

            var packagedFiles = this.LoadPackage("Test.tar.lzma");

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