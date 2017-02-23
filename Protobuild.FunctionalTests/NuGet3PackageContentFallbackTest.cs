namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class NuGet3PackageContentFallbackTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public NuGet3PackageContentFallbackTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("NuGet3PackageContentFallback");

            // This asserts the command succeeds.
            this.OtherMode("resolve", "Windows");
        }
    }
}