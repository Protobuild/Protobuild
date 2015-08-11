namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class SimplifiedFileInclusionWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("SimplifiedFileInclusionWorks");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            Assert.Contains(@"<Compile", projectContents);
            Assert.Contains(@"<Content", projectContents);
            Assert.Contains(@"<None", projectContents);
            Assert.Contains(@"<EmbeddedResource", projectContents);
            Assert.Contains(@"<EmbeddedNativeLibrary", projectContents);
            Assert.Contains(@"<EmbeddedShaderProgram", projectContents);
            Assert.Contains(@"<ShaderProgram", projectContents);
            Assert.Contains(@"<ApplicationDefinition", projectContents);
            Assert.Contains(@"<Page", projectContents);
            Assert.Contains(@"<AppxManifest", projectContents);
            Assert.Contains(@"<BundleResource", projectContents);
            Assert.Contains(@"<InterfaceDefinition", projectContents);
            Assert.Contains(@"<AndroidResource", projectContents);
            Assert.Contains(@"<SplashScreen", projectContents);
            Assert.Contains(@"<Resource", projectContents);
            Assert.Contains(@"<XamarinComponentReference", projectContents);
        }
    }
}