namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ServicesRelativeRequireTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesRelativeRequire");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.Contains("CONSOLE_SERVICE_A;", projectContents);
            Assert.Contains("CONSOLE_SERVICE_B;", projectContents);
        }
    }
}