namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PlatformSpecificOutputDefaultTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PlatformSpecificOutputDefault");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            Assert.Contains(@"bin\Windows\AnyCPU\Release", projectContents);
            Assert.DoesNotContain(@"bin\Release", projectContents);
        }
    }
}