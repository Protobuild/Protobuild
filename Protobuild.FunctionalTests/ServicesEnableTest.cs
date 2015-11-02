namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesEnableTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesEnableTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesEnable");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.DoesNotContain("CONSOLE_SERVICE_A;", consoleContents);

            this.Generate(args: "--enable Console/ServiceA");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            var libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.Contains("CONSOLE_SERVICE_A;", consoleContents);
            _assert.Contains("LIBRARY_SERVICE_B;", libraryContents);

            this.Generate(args: "--enable Library/ServiceB");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.DoesNotContain("CONSOLE_SERVICE_A;", consoleContents);
            _assert.Contains("LIBRARY_SERVICE_B;", libraryContents);
        }
    }
}