namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PlatformSpecificOutputDefaultTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PlatformSpecificOutputDefaultTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PlatformSpecificOutputDefault");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Test\Test.Windows.csproj")));

            var projectContents = this.ReadFile(@"Test\Test.Windows.csproj");

            _assert.Contains(@"bin\Windows\AnyCPU\Release", projectContents);
            _assert.DoesNotContain(@"bin\Release", projectContents);
        }
    }
}