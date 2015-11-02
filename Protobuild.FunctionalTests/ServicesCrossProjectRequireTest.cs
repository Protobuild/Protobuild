namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesCrossProjectRequireTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesCrossProjectRequireTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesCrossProjectRequire");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            var libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.Contains("CONSOLE_SERVICE_A;", consoleContents);
            _assert.Contains("LIBRARY_SERVICE_B;", libraryContents);
        }
    }
}