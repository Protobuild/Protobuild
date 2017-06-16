namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class MacOSPlatformForceAPIMonoMacRejectsUsageTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public MacOSPlatformForceAPIMonoMacRejectsUsageTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("MacOSPlatformForceAPIMonoMacRejectsUsage");

            var @out = this.Generate("MacOS", capture: true, expectFailure: true);
        }
    }
}