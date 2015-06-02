namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class WarningLevelPropertyTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("WarningLevelProperty");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            Assert.Contains(@"<WarningLevel>WARNING_LEVEL_TEST</WarningLevel>", projectContents);
        }
    }
}