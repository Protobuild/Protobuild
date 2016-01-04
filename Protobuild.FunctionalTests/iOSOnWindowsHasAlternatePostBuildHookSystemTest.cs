namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class iOSOnWindowsHasAlternatePostBuildHookSystemTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public iOSOnWindowsHasAlternatePostBuildHookSystemTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("iOSOnWindowsHasAlternatePostBuildHookSystem");

            this.Generate("iOS", hostPlatform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.iOS.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.iOS.csproj");

            _assert.Contains("LocalTouch Path", consoleContents);
            
            this.Generate("tvOS", hostPlatform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.tvOS.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.tvOS.csproj");

            _assert.Contains("LocalTouch Path", consoleContents);

            this.Generate("Windows", hostPlatform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.DoesNotContain("LocalTouch Path", consoleContents);
        }
    }
}