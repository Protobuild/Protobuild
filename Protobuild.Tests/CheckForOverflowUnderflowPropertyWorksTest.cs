namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class CheckForOverflowUnderflowPropertyWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("CheckForOverflowUnderflowPropertyWorks");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.Contains("<CheckForOverflowUnderflow>True</CheckForOverflowUnderflow>", consoleContents);
        }
    }
}