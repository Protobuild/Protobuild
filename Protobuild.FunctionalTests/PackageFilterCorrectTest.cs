namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageFilterCorrectTest : ProtobuildTest
    {
        [Fact]
        public void PackageIsCorrect()
        {
            this.SetupTest("PackageFilterCorrect", isPackTest: true);

            this.OtherMode("pack", args: ". Windows.tar.lzma Windows Build/Publish/Filter.Windows.txt", purge: false);

            var packagedFiles = this.LoadPackage("Windows.tar.lzma");

            Assert.Contains("Build/", packagedFiles.Keys);
            Assert.Contains("Build/Module.xml", packagedFiles.Keys);
            Assert.Contains("Build/Projects/", packagedFiles.Keys);
            Assert.Contains("Build/Projects/Console.Windows.definition", packagedFiles.Keys);
            Assert.Contains("Console/", packagedFiles.Keys);
            Assert.Contains("Console/Program.cs", packagedFiles.Keys);
            Assert.Equal(6, packagedFiles.Count);

            using (var stream = new MemoryStream(packagedFiles["Build/Projects/Console.Windows.definition"]))
            {
                using (var reader = new StreamReader(stream))
                {
                    Assert.Contains("WINDOWS", reader.ReadToEnd());
                }
            }

            this.OtherMode("pack", args: ". Linux.tar.lzma Linux Build/Publish/Filter.Linux.txt", purge: false);

            packagedFiles = this.LoadPackage("Linux.tar.lzma");

            Assert.Contains("Build/", packagedFiles.Keys);
            Assert.Contains("Build/Module.xml", packagedFiles.Keys);
            Assert.Contains("Build/Projects/", packagedFiles.Keys);
            Assert.Contains("Build/Projects/Console.Linux.definition", packagedFiles.Keys);
            Assert.Contains("Console/", packagedFiles.Keys);
            Assert.Contains("Console/Program.cs", packagedFiles.Keys);
            Assert.Equal(6, packagedFiles.Count);

            using (var stream = new MemoryStream(packagedFiles["Build/Projects/Console.Linux.definition"]))
            {
                using (var reader = new StreamReader(stream))
                {
                    Assert.Contains("LINUX", reader.ReadToEnd());
                }
            }
        }
    }
}