namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ExternalProjectReferencesAreFlattenedTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ExternalProjectReferencesAreFlattened");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.DoesNotContain("ExternalA", consoleContents);
            Assert.DoesNotContain("ExternalB", consoleContents);
            Assert.DoesNotContain("ExternalC", consoleContents);
            Assert.DoesNotContain("ExternalD", consoleContents);
            Assert.DoesNotContain("ExternalE", consoleContents);
            Assert.Contains("ExpectedTargetMet", consoleContents);
            Assert.DoesNotContain("UnexpectedTargetPresent", consoleContents);
        }
    }
}