namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PostBuildHookHasHostPlatformTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PostBuildHookHasHostPlatformTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PostBuildHookHasHostPlatform");

            this.Generate("Android", hostPlatform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Android.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Hook\Hook.Android.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Hook.Library\Hook.Library.Android.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Android.csproj");
            var hookContents = this.ReadFile(@"Hook\Hook.Android.csproj");
            var hookLibraryContents = this.ReadFile(@"Hook.Library\Hook.Library.Android.csproj");

            _assert.Contains(@"bin\Windows\$(Platform)\$(Configuration)\Hook.exe", consoleContents);
            _assert.DoesNotContain(@"bin\Android\$(Platform)\$(Configuration)\Hook.exe", consoleContents);
        }
    }
}