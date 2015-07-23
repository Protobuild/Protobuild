namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ForcePCLGenerationWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ForcePCLGenerationWorks");

            this.Generate("Windows");
        }
    }
}