namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ServicesDefaultForRootTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesDefaultForRoot");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.Contains("CONSOLE_SERVICE;", projectContents);
        }
    }
}