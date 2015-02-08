namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ResyncPreventedWhenSyncIsDisabledTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ResyncPreventedWhenSyncIsDisabled");

            this.OtherMode("generate", "Windows");

            this.OtherMode("resync", "Windows", expectFailure: true);
        }
    }
}