namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class DuplicateExternalProjectsTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("DuplicateExternalProjects");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"DuplicateExternalProjects.Windows.sln")));

            var solutionContents = this.ReadFile(@"DuplicateExternalProjects.Windows.sln");

            Assert.Contains("ThirdParty1.csproj", solutionContents);
            Assert.DoesNotContain("ThirdParty2.csproj", solutionContents);
        }
    }
}