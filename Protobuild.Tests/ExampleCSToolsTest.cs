namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ExampleCSToolsTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ExampleCSTools");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Game\Game.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"CSTools\Tool\Tool.Windows.csproj")));

            var gameContents = this.ReadFile(@"Game\Game.Windows.csproj");
            var solutionContents = this.ReadFile(@"Game.Windows.sln");

            Assert.DoesNotContain("Tool.Windows.csproj", gameContents);
            Assert.Contains("Game.Windows.csproj", solutionContents);
            Assert.Contains("Tool.Windows.csproj", solutionContents);
        }
    }
}