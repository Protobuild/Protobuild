namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExternalProjectReferencesAreFlattenedTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExternalProjectReferencesAreFlattenedTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ExternalProjectReferencesAreFlattened");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.DoesNotContain("ExternalA", consoleContents);
            _assert.DoesNotContain("ExternalB", consoleContents);
            _assert.DoesNotContain("ExternalC", consoleContents);
            _assert.DoesNotContain("ExternalD", consoleContents);
            _assert.DoesNotContain("ExternalE", consoleContents);
            _assert.Contains("ExpectedTargetMet", consoleContents);
            _assert.DoesNotContain("UnexpectedTargetPresent", consoleContents);
        }
    }
}