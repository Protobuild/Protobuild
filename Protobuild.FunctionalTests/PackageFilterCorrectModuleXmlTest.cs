namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageFilterCorrectModuleXmlTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageFilterCorrectModuleXmlTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void PackageIsCorrect()
        {
            this.SetupTest("PackageFilterCorrectModuleXml", isPackTest: true);

            this.OtherMode("pack", args: ". Windows.tar.lzma Windows Filter.Windows.txt", purge: false);

            var packagedFiles = this.LoadPackage("Windows.tar.lzma");

            _assert.Contains("Build/", packagedFiles.Keys);
            _assert.Contains("Build/Module.xml", packagedFiles.Keys);
            _assert.Contains("Build/Projects/", packagedFiles.Keys);
            _assert.Contains("Build/Projects/Console.definition", packagedFiles.Keys);
            _assert.Equal(4, packagedFiles.Count);
        }
    }
}