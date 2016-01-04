namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class TouchTaskGetsSessionIdForiOSOnWindowsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public TouchTaskGetsSessionIdForiOSOnWindowsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("TouchTaskGetsSessionIdForiOSOnWindows");

            this.Generate("iOS", hostPlatform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.iOS.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.iOS.csproj");

            _assert.Contains("SessionId=\"$(BuildSessionId)\"", consoleContents);
            
            this.Generate("tvOS", hostPlatform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.tvOS.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.tvOS.csproj");

            _assert.Contains("SessionId=\"$(BuildSessionId)\"", consoleContents);

            this.Generate("Windows", hostPlatform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.DoesNotContain("SessionId=\"$(BuildSessionId)\"", consoleContents);
        }
    }
}