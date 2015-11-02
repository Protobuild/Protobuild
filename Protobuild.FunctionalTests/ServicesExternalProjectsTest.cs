namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesExternalProjectsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesExternalProjectsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesExternalProjects");

            this.Generate(platform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("RefGACNoPlatform", consoleContents);
            _assert.Contains("RefBinaryNoPlatform", consoleContents);
            _assert.Contains("RefProjectNoPlatform", consoleContents);
            _assert.Contains("RefProtobuildNoPlatform", consoleContents);
            _assert.Contains("RefGACPlatform", consoleContents);
            _assert.Contains("RefBinaryPlatform", consoleContents);
            _assert.Contains("RefProjectPlatform", consoleContents);
            _assert.Contains("RefProtobuildPlatform", consoleContents);

            this.Generate(platform: "Linux");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Linux.csproj");

            _assert.Contains("RefGACNoPlatform", consoleContents);
            _assert.Contains("RefBinaryNoPlatform", consoleContents);
            _assert.Contains("RefProjectNoPlatform", consoleContents);
            _assert.Contains("RefProtobuildNoPlatform", consoleContents);
            _assert.DoesNotContain("RefGACPlatform", consoleContents);
            _assert.DoesNotContain("RefBinaryPlatform", consoleContents);
            _assert.DoesNotContain("RefProjectPlatform", consoleContents);
            _assert.DoesNotContain("RefProtobuildPlatform", consoleContents);
        }
    }
}