namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class NuGetPackagesDetectedFromParentWithRootPathDotTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public NuGetPackagesDetectedFromParentWithRootPathDotTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("NuGetPackagesDetectedFromParentWithRootPathDot");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Submodule\Console.Windows.csproj");

            _assert.Contains("portable-net4+sl5+wp8+win8+wpa81+MonoTouch+MonoAndroid", consoleContents);
            _assert.Contains("Test.dll", consoleContents);
            _assert.Contains("<HintPath>..\\packages", consoleContents);
            _assert.DoesNotContain("<HintPath>packages", consoleContents);
            _assert.DoesNotContain("<HintPath>..\\..\\packages", consoleContents);
        }
    }
}