namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class DefaultStartupProjectTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("DefaultProjectNotSet");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));

            var solutionContents = this.ReadFile(@"Module.Windows.sln");
            var consoleAIndex = solutionContents.IndexOf("ConsoleA");
            var consoleBIndex = solutionContents.IndexOf("ConsoleB");

            Assert.NotEqual(-1, consoleAIndex);
            Assert.NotEqual(-1, consoleBIndex);
            Assert.True(consoleAIndex < consoleBIndex, "Console A must appear before Console B");

            this.SetupTest("DefaultProjectExplicitlySet");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));

            solutionContents = this.ReadFile(@"Module.Windows.sln");
            consoleAIndex = solutionContents.IndexOf("ConsoleA");
            consoleBIndex = solutionContents.IndexOf("ConsoleB");

            Assert.NotEqual(-1, consoleAIndex);
            Assert.NotEqual(-1, consoleBIndex);
            Assert.True(consoleBIndex < consoleAIndex, "Console B must appear before Console A");
        }
    }
}