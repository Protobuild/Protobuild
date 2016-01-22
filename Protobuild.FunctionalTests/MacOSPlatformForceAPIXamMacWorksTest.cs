namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class MacOSPlatformForceAPIXamMacWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public MacOSPlatformForceAPIXamMacWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("MacOSPlatformForceAPIXamMacWorks");

            var @out = this.Generate("MacOS", capture: true);

            var appPath = this.GetPath("App\\App.MacOS.csproj");

            _assert.True(File.Exists(appPath));

            var appContents = this.ReadFile(appPath);

            _assert.Contains("<Reference Include=\"XamMac\"", appContents);
            _assert.Contains("{42C0BBD9-55CE-4FC1-8D90-A7348ABAFB23}", appContents);
            _assert.DoesNotContain("Xamarin.Mac.CSharp.targets", appContents);
            _assert.Contains("PLATFORM_MACOS_LEGACY", appContents);
        }
    }
}