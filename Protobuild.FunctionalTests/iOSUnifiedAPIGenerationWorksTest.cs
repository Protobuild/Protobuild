namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class iOSUnifiedAPIGenerationWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public iOSUnifiedAPIGenerationWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("iOSUnifiedAPIGenerationWorks");

            this.Generate("iOS");

            _assert.True(File.Exists(this.GetPath(@"App\App.iOS.csproj")));

            var appContents = this.ReadFile(@"App\App.iOS.csproj");

            _assert.Contains(@"Xamarin.iOS", appContents);
            _assert.Contains(@"$(MSBuildExtensionsPath)\Xamarin\iOS\Xamarin.iOS.CSharp.targets", appContents);
            _assert.DoesNotContain(@"monotouch", appContents);
            _assert.DoesNotContain(@"$(MSBuildToolsPath)\Microsoft.CSharp.targets", appContents);
        }
    }
}