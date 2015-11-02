namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageEverythingCorrectGZipTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageEverythingCorrectGZipTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingCorrectGZip", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.gz Windows Filter.txt --format tar/gzip", purge: false);

            var packagedFiles = this.LoadPackage("Test.tar.gz");

            _assert.Contains("Build/", packagedFiles.Keys);
            _assert.Contains("Build/Module.xml", packagedFiles.Keys);
            _assert.Contains("Build/Projects/", packagedFiles.Keys);
            _assert.Contains("Build/Projects/Console.definition", packagedFiles.Keys);
            _assert.Contains("Console/", packagedFiles.Keys);
            _assert.Contains("Console/Program.cs", packagedFiles.Keys);
            _assert.Equal(6, packagedFiles.Count);
        }
    }
}