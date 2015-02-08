namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ServicesDisableTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesDisable");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            var libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            Assert.Contains("CONSOLE_SERVICE_A;", consoleContents);
            Assert.Contains("LIBRARY_SERVICE_B;", libraryContents);

            this.Generate(args: "--disable Console/ServiceA");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.DoesNotContain("CONSOLE_SERVICE_A;", consoleContents);

            // Protobuild now returns with exit code 1 if you try to disable
            // a service that is required.
            this.Generate(args: "--disable Library/ServiceB", expectFailure: true);
        }
    }
}