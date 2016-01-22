namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class MacOSPlatformForceAPIMonoMacWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public MacOSPlatformForceAPIMonoMacWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("MacOSPlatformForceAPIMonoMacWorks");

            var @out = this.Generate("MacOS", capture: true);

            var appPath = this.GetPath("App\\App.MacOS.csproj");

            _assert.True(File.Exists(appPath));

            var appContents = this.ReadFile(appPath);

            _assert.Contains("<Reference Include=\"MonoMac\"", appContents);
            _assert.Contains("{948B3504-5B70-4649-8FE4-BDE1FB46EC69}", appContents);
            _assert.DoesNotContain("Xamarin.Mac.CSharp.targets", appContents);
            _assert.Contains("PLATFORM_MACOS_LEGACY", appContents);
        }
    }
}