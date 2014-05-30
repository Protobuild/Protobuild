namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class NotDefaultForRootTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesNotDefaultForRoot");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.DoesNotContain("CONSOLE_SERVICE;", projectContents);
        }
    }
}