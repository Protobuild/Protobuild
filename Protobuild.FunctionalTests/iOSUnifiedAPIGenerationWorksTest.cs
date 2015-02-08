namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class iOSUnifiedAPIGenerationWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("iOSUnifiedAPIGenerationWorks");

            this.Generate("iOS");

            Assert.True(File.Exists(this.GetPath(@"App\App.iOS.csproj")));

            var appContents = this.ReadFile(@"App\App.iOS.csproj");

            Assert.Contains(@"Xamarin.iOS", appContents);
            Assert.Contains(@"$(MSBuildExtensionsPath)\Xamarin\iOS\Xamarin.iOS.CSharp.targets", appContents);
            Assert.DoesNotContain(@"monotouch", appContents);
            Assert.DoesNotContain(@"$(MSBuildToolsPath)\Microsoft.CSharp.targets", appContents);
        }
    }
}