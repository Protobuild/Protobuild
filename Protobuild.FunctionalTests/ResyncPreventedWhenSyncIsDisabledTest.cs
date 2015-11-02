namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ResyncPreventedWhenSyncIsDisabledTest : ProtobuildTest
    {
        public ResyncPreventedWhenSyncIsDisabledTest(IAssert assert) : base(assert)
        {
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ResyncPreventedWhenSyncIsDisabled");

            this.OtherMode("generate", "Windows");

            this.OtherMode("resync", "Windows", expectFailure: true);
        }
    }
}