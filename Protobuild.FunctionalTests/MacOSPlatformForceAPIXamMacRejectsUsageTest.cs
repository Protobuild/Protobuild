namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class MacOSPlatformForceAPIXamMacRejectsUsageTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public MacOSPlatformForceAPIXamMacRejectsUsageTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("MacOSPlatformForceAPIXamMacRejectsUsage");

            var @out = this.Generate("MacOS", capture: true, expectFailure: true);
        }
    }
}