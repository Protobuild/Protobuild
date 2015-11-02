namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesRelativeRequireTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesRelativeRequireTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesRelativeRequire");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("CONSOLE_SERVICE_A;", projectContents);
            _assert.Contains("CONSOLE_SERVICE_B;", projectContents);
        }
    }
}