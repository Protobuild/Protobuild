namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class WarningLevelPropertyTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public WarningLevelPropertyTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("WarningLevelProperty");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            _assert.Contains(@"<WarningLevel>WARNING_LEVEL_TEST</WarningLevel>", projectContents);
        }
    }
}