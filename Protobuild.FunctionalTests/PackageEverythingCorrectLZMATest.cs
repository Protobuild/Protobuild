namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageEverythingCorrectLZMATest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageEverythingCorrectLZMATest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingCorrectLZMA", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.lzma Windows Filter.txt --format tar/lzma", purge: false);

            var packagedFiles = this.LoadPackage("Test.tar.lzma");

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