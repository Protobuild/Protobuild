namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class DependenciesTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesDependencies");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Submodule.Windows.sln")));

            var moduleContents = this.ReadFile(@"Module.Windows.sln");
            var submoduleContents = this.ReadFile(@"Submodule\Submodule.Windows.sln");

            Assert.Contains("Console.Windows.csproj", moduleContents);
            Assert.Contains("Library.Windows.csproj", moduleContents);
            Assert.Contains("Library.Windows.csproj", submoduleContents);
        }
    }
}