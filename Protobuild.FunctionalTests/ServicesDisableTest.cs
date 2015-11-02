namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesDisableTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesDisableTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesDisable");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\LibraryExcludable.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            var libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");
            var libraryExcludableContents = this.ReadFile(@"Submodule\Library\LibraryExcludable.Windows.csproj");

            _assert.Contains("CONSOLE_SERVICE_A;", consoleContents);
            _assert.Contains("LIBRARY_SERVICE_B;", libraryContents);
            _assert.Contains("LIBRARY_SERVICE_C;", libraryContents);
            _assert.Contains("LIBRARY_SERVICE_D;", libraryExcludableContents);

            this.Generate(args: "--disable Console/ServiceA");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath(@"Submodule\Library\LibraryExcludable.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.DoesNotContain("CONSOLE_SERVICE_A;", consoleContents);
            _assert.DoesNotContain("LIBRARY_SERVICE_B;", libraryContents);
            _assert.Contains("LIBRARY_SERVICE_C;", libraryContents);

            this.Generate(args: "--disable Library/ServiceB");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath(@"Submodule\Library\LibraryExcludable.Windows.csproj")));

            consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");
            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.DoesNotContain("CONSOLE_SERVICE_A;", consoleContents);
            _assert.DoesNotContain("LIBRARY_SERVICE_B;", libraryContents);
            _assert.Contains("LIBRARY_SERVICE_C;", libraryContents);

            // Protobuild now returns with exit code 1 if you try to disable
            // a service that is required.
            this.Generate(args: "--disable Library/ServiceC", expectFailure: true);
        }
    }
}