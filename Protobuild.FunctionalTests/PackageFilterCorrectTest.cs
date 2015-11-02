namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageFilterCorrectTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageFilterCorrectTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void PackageIsCorrect()
        {
            this.SetupTest("PackageFilterCorrect", isPackTest: true);

            this.OtherMode("pack", args: ". Windows.tar.lzma Windows Build/Publish/Filter.Windows.txt", purge: false);

            var packagedFiles = this.LoadPackage("Windows.tar.lzma");

            _assert.Contains("Build/", packagedFiles.Keys);
            _assert.Contains("Build/Module.xml", packagedFiles.Keys);
            _assert.Contains("Build/Projects/", packagedFiles.Keys);
            _assert.Contains("Build/Projects/Console.Windows.definition", packagedFiles.Keys);
            _assert.Contains("Console/", packagedFiles.Keys);
            _assert.Contains("Console/Program.cs", packagedFiles.Keys);
            _assert.Equal(6, packagedFiles.Count);

            using (var stream = new MemoryStream(packagedFiles["Build/Projects/Console.Windows.definition"]))
            {
                using (var reader = new StreamReader(stream))
                {
                    _assert.Contains("WINDOWS", reader.ReadToEnd());
                }
            }

            this.OtherMode("pack", args: ". Linux.tar.lzma Linux Build/Publish/Filter.Linux.txt", purge: false);

            packagedFiles = this.LoadPackage("Linux.tar.lzma");

            _assert.Contains("Build/", packagedFiles.Keys);
            _assert.Contains("Build/Module.xml", packagedFiles.Keys);
            _assert.Contains("Build/Projects/", packagedFiles.Keys);
            _assert.Contains("Build/Projects/Console.Linux.definition", packagedFiles.Keys);
            _assert.Contains("Console/", packagedFiles.Keys);
            _assert.Contains("Console/Program.cs", packagedFiles.Keys);
            _assert.Equal(6, packagedFiles.Count);

            using (var stream = new MemoryStream(packagedFiles["Build/Projects/Console.Linux.definition"]))
            {
                using (var reader = new StreamReader(stream))
                {
                    _assert.Contains("LINUX", reader.ReadToEnd());
                }
            }
        }
    }
}