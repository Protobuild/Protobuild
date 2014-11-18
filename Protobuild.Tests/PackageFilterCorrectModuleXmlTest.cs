namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageFilterCorrectModuleXmlTest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("PackageFilterCorrectModuleXml", isPackTest: true);

            this.OtherMode("pack", args: ". Windows.tar.lzma Windows Filter.Windows.txt", purge: false);

            var packagedFiles = this.LoadPackage("Windows.tar.lzma");

            Assert.Contains("Build/", packagedFiles.Keys);
            Assert.Contains("Build/Module.xml", packagedFiles.Keys);
            Assert.Contains("Build/Projects/", packagedFiles.Keys);
            Assert.Contains("Build/Projects/Console.definition", packagedFiles.Keys);
            Assert.Equal(4, packagedFiles.Count);
        }
    }
}