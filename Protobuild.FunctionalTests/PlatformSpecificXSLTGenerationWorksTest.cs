namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PlatformSpecificXSLTGenerationWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PlatformSpecificXSLTGenerationWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PlatformSpecificXSLTGenerationWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("ToolsVersion", consoleContents);

            this.Generate("MyCustomPlatform");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.MyCustomPlatform.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.MyCustomPlatform.csproj");

            _assert.Contains("MY_CUSTOM_XML", consoleContents);
        }
    }
}