namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PostBuildHookWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PostBuildHookWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PostBuildHookWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Hook\Hook.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Hook.Library\Hook.Library.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            var hookContents = this.ReadFile(@"Hook\Hook.Windows.csproj");
            var hookLibraryContents = this.ReadFile(@"Hook.Library\Hook.Library.Windows.csproj");

            _assert.Contains(@"Running &quot;Hook&quot; post-build hook...", consoleContents);
            _assert.Contains(@"Running &quot;Hook.External&quot; post-build hook...", consoleContents);
            _assert.DoesNotContain(@"Running &quot;Hook&quot; post-build hook...", hookContents);
            _assert.Contains(@"Running &quot;Hook.External&quot; post-build hook...", hookContents);
            _assert.DoesNotContain(@"Running &quot;Hook&quot; post-build hook...", hookLibraryContents);
            _assert.Contains(@"Running &quot;Hook.External&quot; post-build hook...", hookLibraryContents);
        }
    }
}