namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class BasicGenerationWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("BasicGenerationWorks");

            this.Generate("Windows");
        }
    }
}