namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesDefaultForRootTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesDefaultForRootTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesDefaultForRoot");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("CONSOLE_SERVICE;", projectContents);
        }
    }
}