namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class MacOSPlatformForceAPIXamarinMacWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public MacOSPlatformForceAPIXamarinMacWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("MacOSPlatformForceAPIXamarinMacWorks");

            var @out = this.Generate("MacOS", capture: true);

            var appPath = this.GetPath("App\\App.MacOS.csproj");

            _assert.True(File.Exists(appPath));

            var appContents = this.ReadFile(appPath);

            _assert.Contains("<Reference Include=\"Xamarin.Mac\"", appContents);
            _assert.Contains("{A3F8F2AB-B479-4A4A-A458-A89E7DC349F1}", appContents);
            _assert.Contains("Xamarin.Mac.CSharp.targets", appContents);
            _assert.DoesNotContain("PLATFORM_MACOS_LEGACY", appContents);
        }
    }
}