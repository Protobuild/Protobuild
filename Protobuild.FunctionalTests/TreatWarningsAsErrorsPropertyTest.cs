namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class TreatWarningsAsErrorsPropertyTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("TreatWarningsAsErrorsProperty");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            Assert.Contains(@"<TreatWarningsAsErrors>TREAT_WARNINGS_AS_ERRORS_TEST</TreatWarningsAsErrors>", projectContents);
        }
    }
}