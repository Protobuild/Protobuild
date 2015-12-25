namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageProtobuildHTTPResolvesSecondTimeForSourceOnlyTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageProtobuildHTTPResolvesSecondTimeForSourceOnlyTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageProtobuildHTTPResolvesSecondTimeForSourceOnly");

            this.OtherMode("resolve", "Windows");
            this.OtherMode("resolve", "Windows");
        }
    }
}