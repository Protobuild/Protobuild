namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PlatformSpecificXSLTGenerationWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PlatformSpecificXSLTGenerationWorks");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.Contains("ToolsVersion", consoleContents);

            this.Generate("MyCustomPlatform");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.MyCustomPlatform.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.MyCustomPlatform.csproj");

            Assert.Contains("MY_CUSTOM_XML", consoleContents);
        }
    }
}