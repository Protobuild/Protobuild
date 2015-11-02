namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesDependenciesTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesDependenciesTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesDependencies");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Submodule.Windows.sln")));

            var moduleContents = this.ReadFile(@"Module.Windows.sln");
            var submoduleContents = this.ReadFile(@"Submodule\Submodule.Windows.sln");

            _assert.Contains("Console.Windows.csproj", moduleContents);
            _assert.Contains("Library.Windows.csproj", moduleContents);
            _assert.Contains("Library.Windows.csproj", submoduleContents);
        }
    }
}