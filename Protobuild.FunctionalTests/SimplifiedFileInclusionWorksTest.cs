namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class SimplifiedFileInclusionWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public SimplifiedFileInclusionWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("SimplifiedFileInclusionWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            _assert.Contains(@"<Compile", projectContents);
            _assert.Contains(@"<Content", projectContents);
            _assert.Contains(@"<None", projectContents);
            _assert.Contains(@"<EmbeddedResource", projectContents);
            _assert.Contains(@"<EmbeddedNativeLibrary", projectContents);
            _assert.Contains(@"<EmbeddedShaderProgram", projectContents);
            _assert.Contains(@"<ShaderProgram", projectContents);
            _assert.Contains(@"<ApplicationDefinition", projectContents);
            _assert.Contains(@"<Page", projectContents);
            _assert.Contains(@"<AppxManifest", projectContents);
            _assert.Contains(@"<BundleResource", projectContents);
            _assert.Contains(@"<InterfaceDefinition", projectContents);
            _assert.Contains(@"<AndroidResource", projectContents);
            _assert.Contains(@"<SplashScreen", projectContents);
            _assert.Contains(@"<Resource", projectContents);
            _assert.Contains(@"<XamarinComponentReference", projectContents);
        }
    }
}