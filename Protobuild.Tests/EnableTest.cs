namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class EnableTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesEnable");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.DoesNotContain("CONSOLE_SERVICE_A;", consoleContents);

            this.Generate(args: "--enable Console/ServiceA");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            var libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            Assert.Contains("CONSOLE_SERVICE_A;", consoleContents);
            Assert.Contains("LIBRARY_SERVICE_B;", libraryContents);

            this.Generate(args: "--enable Library/ServiceB");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            Assert.DoesNotContain("CONSOLE_SERVICE_A;", consoleContents);
            Assert.Contains("LIBRARY_SERVICE_B;", libraryContents);
        }
    }
}