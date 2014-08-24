namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PlatformSpecificOutputFalseTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PlatformSpecificOutputFalse");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            Assert.DoesNotContain(@"bin\Windows\AnyCPU\Release", projectContents);
            Assert.Contains(@"bin\Release", projectContents);
        }
    }
}