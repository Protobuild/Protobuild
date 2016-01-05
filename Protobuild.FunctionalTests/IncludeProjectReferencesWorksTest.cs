namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class IncludeProjectReferencesWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public IncludeProjectReferencesWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("IncludeProjectReferencesWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath(@"Includable\Includable.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<Reference Include=\"MyGACAssembly\"", projectContents);
        }
    }
}