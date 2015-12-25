namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageProtobuildHTTPSResolvesNewForSourceOnlyTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageProtobuildHTTPSResolvesNewForSourceOnlyTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageProtobuildHTTPSResolvesNewForSourceOnly");

            this.OtherMode("resolve", "Windows");
        }
    }
}