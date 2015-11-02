namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PlatformSpecificOutputFalseTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PlatformSpecificOutputFalseTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PlatformSpecificOutputFalse");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            _assert.DoesNotContain(@"bin\Windows\AnyCPU\Release", projectContents);
            _assert.Contains(@"bin\Release", projectContents);
        }
    }
}