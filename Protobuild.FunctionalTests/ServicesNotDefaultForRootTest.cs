namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesNotDefaultForRootTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesNotDefaultForRootTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesNotDefaultForRoot");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.DoesNotContain("CONSOLE_SERVICE;", projectContents);
        }
    }
}