namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageEverythingFailWhenBuildNotPresentTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageEverythingFailWhenBuildNotPresentTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void PackageIsCorrect()
        {
            this.SetupTest("PackageEverythingFailWhenBuildNotPresent", isPackTest: true);

            this.OtherMode("pack", args: "Publish Test.tar.lzma Windows Filter.txt --format tar/lzma", expectFailure: true, purge: false);
        }
    }
}