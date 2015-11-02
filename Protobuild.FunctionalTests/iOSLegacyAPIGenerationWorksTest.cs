namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class iOSLegacyAPIGenerationWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public iOSLegacyAPIGenerationWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("iOSLegacyAPIGenerationWorks");

            this.Generate("iOS");

            _assert.True(File.Exists(this.GetPath(@"App\App.iOS.csproj")));

            var appContents = this.ReadFile(@"App\App.iOS.csproj");

            _assert.DoesNotContain(@"Xamarin.iOS", appContents);
            _assert.DoesNotContain(@"$(MSBuildExtensionsPath)\Xamarin\iOS\Xamarin.iOS.CSharp.targets", appContents);
            _assert.Contains(@"monotouch", appContents);
            _assert.Contains(@"$(MSBuildToolsPath)\Microsoft.CSharp.targets", appContents);
        }
    }
}