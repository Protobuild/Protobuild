namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ServicesDependenciesPlatformsTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesDependenciesPlatforms");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Submodule.Windows.sln")));

            var moduleContents = this.ReadFile(@"Module.Windows.sln");
            var submoduleContents = this.ReadFile(@"Submodule\Submodule.Windows.sln");

            Assert.Contains("Console.Windows.csproj", moduleContents);
            Assert.Contains("Library.Windows.csproj", moduleContents);
            Assert.Contains("Library.Windows.csproj", submoduleContents);

            this.Generate("Linux");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Module.Linux.sln")));
            Assert.False(File.Exists(this.GetPath(@"Submodule\Library\Library.Linux.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Submodule.Linux.sln")));

            moduleContents = this.ReadFile(@"Module.Linux.sln");
            submoduleContents = this.ReadFile(@"Submodule\Submodule.Linux.sln");

            Assert.Contains("Console.Linux.csproj", moduleContents);
            Assert.DoesNotContain("Library.Linux.csproj", moduleContents);
            Assert.DoesNotContain("Library.Linux.csproj", submoduleContents);
        }
    }
}