namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class iOSLegacyAPIGenerationWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("iOSLegacyAPIGenerationWorks");

            this.Generate("iOS");

            Assert.True(File.Exists(this.GetPath(@"App\App.iOS.csproj")));

            var appContents = this.ReadFile(@"App\App.iOS.csproj");

            Assert.DoesNotContain(@"Xamarin.iOS", appContents);
            Assert.DoesNotContain(@"$(MSBuildExtensionsPath)\Xamarin\iOS\Xamarin.iOS.CSharp.targets", appContents);
            Assert.Contains(@"monotouch", appContents);
            Assert.Contains(@"$(MSBuildToolsPath)\Microsoft.CSharp.targets", appContents);
        }
    }
}