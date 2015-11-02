namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesDependenciesPlatformsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesDependenciesPlatformsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesDependenciesPlatforms");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Submodule.Windows.sln")));

            var moduleContents = this.ReadFile(@"Module.Windows.sln");
            var submoduleContents = this.ReadFile(@"Submodule\Submodule.Windows.sln");

            _assert.Contains("Console.Windows.csproj", moduleContents);
            _assert.Contains("Library.Windows.csproj", moduleContents);
            _assert.Contains("Library.Windows.csproj", submoduleContents);

            this.Generate("Linux");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Module.Linux.sln")));
            _assert.False(File.Exists(this.GetPath(@"Submodule\Library\Library.Linux.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Submodule.Linux.sln")));

            moduleContents = this.ReadFile(@"Module.Linux.sln");
            submoduleContents = this.ReadFile(@"Submodule\Submodule.Linux.sln");

            _assert.Contains("Console.Linux.csproj", moduleContents);
            _assert.DoesNotContain("Library.Linux.csproj", moduleContents);
            _assert.DoesNotContain("Library.Linux.csproj", submoduleContents);
        }
    }
}