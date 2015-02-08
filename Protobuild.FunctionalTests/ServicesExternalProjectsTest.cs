namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ServicesExternalProjectsTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesExternalProjects");

            this.Generate(platform: "Windows");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.Contains("RefGACNoPlatform", consoleContents);
            Assert.Contains("RefBinaryNoPlatform", consoleContents);
            Assert.Contains("RefProjectNoPlatform", consoleContents);
            Assert.Contains("RefProtobuildNoPlatform", consoleContents);
            Assert.Contains("RefGACPlatform", consoleContents);
            Assert.Contains("RefBinaryPlatform", consoleContents);
            Assert.Contains("RefProjectPlatform", consoleContents);
            Assert.Contains("RefProtobuildPlatform", consoleContents);

            this.Generate(platform: "Linux");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Linux.csproj");

            Assert.Contains("RefGACNoPlatform", consoleContents);
            Assert.Contains("RefBinaryNoPlatform", consoleContents);
            Assert.Contains("RefProjectNoPlatform", consoleContents);
            Assert.Contains("RefProtobuildNoPlatform", consoleContents);
            Assert.DoesNotContain("RefGACPlatform", consoleContents);
            Assert.DoesNotContain("RefBinaryPlatform", consoleContents);
            Assert.DoesNotContain("RefProjectPlatform", consoleContents);
            Assert.DoesNotContain("RefProtobuildPlatform", consoleContents);
        }
    }
}