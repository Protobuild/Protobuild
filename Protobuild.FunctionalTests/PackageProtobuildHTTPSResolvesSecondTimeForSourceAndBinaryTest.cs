namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageProtobuildHTTPSResolvesSecondTimeForSourceAndBinaryTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageProtobuildHTTPSResolvesSecondTimeForSourceAndBinaryTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageProtobuildHTTPSResolvesSecondTimeForSourceAndBinary");

            this.OtherMode("resolve", "Windows");
            this.OtherMode("resolve", "Windows");
        }
    }
}