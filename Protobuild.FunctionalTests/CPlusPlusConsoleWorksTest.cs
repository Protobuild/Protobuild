namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class CPlusPlusConsoleWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public CPlusPlusConsoleWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("CPlusPlusConsoleWorks");

            this.Generate("Windows");

            _assert.True(
                File.Exists(this.GetPath(@"Console\Console.Windows.cproj")) ||
                File.Exists(this.GetPath(@"Console\Console.Windows.vcxproj")));

            if (File.Exists(this.GetPath(@"Console\Console.Windows.cproj")))
            {
                var consoleContents = this.ReadFile(@"Console\Console.Windows.cproj");

                // Looks for various C++ specific configuration values.
                _assert.Contains("main.c", consoleContents);
                _assert.Contains("SourceDirectory", consoleContents);
                _assert.Contains("DefineSymbols", consoleContents);
                _assert.Contains("GccCompiler", consoleContents);
            }
            else if (File.Exists(this.GetPath(@"Console\Console.Windows.vcxproj")))
            {
                var consoleContents = this.ReadFile(@"Console\Console.Windows.vcxproj");

                // TODO: Validate Visual Studio C++ generated project.
            }
        }
    }
}