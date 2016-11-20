namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageUnifiedNuGetTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageUnifiedNuGetTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void CommandRunsSuccessfully()
        {
            this.SetupTest("PackageUnifiedNuGet");

            this.OtherMode("automated-build");
        }
    }
}
