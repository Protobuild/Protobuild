namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class CPlusPlusConsoleWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("CPlusPlusConsoleWorks");

            this.Generate("Windows");

            Assert.True(
                File.Exists(this.GetPath(@"Console\Console.Windows.cproj")) ||
                File.Exists(this.GetPath(@"Console\Console.Windows.vcxproj")));

            if (File.Exists(this.GetPath(@"Console\Console.Windows.cproj")))
            {
                var consoleContents = this.ReadFile(@"Console\Console.Windows.cproj");

                // Looks for various C++ specific configuration values.
                Assert.Contains("main.c", consoleContents);
                Assert.Contains("SourceDirectory", consoleContents);
                Assert.Contains("DefineSymbols", consoleContents);
                Assert.Contains("GccCompiler", consoleContents);
            }
            else if (File.Exists(this.GetPath(@"Console\Console.Windows.vcxproj")))
            {
                var consoleContents = this.ReadFile(@"Console\Console.Windows.vcxproj");

                // TODO: Validate Visual Studio C++ generated project.
            }
        }
    }
}