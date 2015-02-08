namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class BasicSynchronisationWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("BasicSynchronisationWorks");

            this.OtherMode("resync", "Linux");
            this.OtherMode("resync", "Linux", purge: false);
            this.OtherMode("resync", "Linux", purge: false);
        }
    }
}