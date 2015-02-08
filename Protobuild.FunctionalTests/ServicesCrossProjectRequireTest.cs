namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ServicesCrossProjectRequireTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesCrossProjectRequire");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            var libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            Assert.Contains("CONSOLE_SERVICE_A;", consoleContents);
            Assert.Contains("LIBRARY_SERVICE_B;", libraryContents);
        }
    }
}