namespace Protobuild.Tests
{
    using System;
    using System.IO;
    using Prototest.Library.Version1;
    
    public class CPlusPlusDirectReferenceWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public CPlusPlusDirectReferenceWorksTest(IAssert assert, ICategorize categorize) : base(assert)
        {
            _assert = assert;

            categorize.Method("CPlusPlusPotentialSWIGInstallation", () => GenerationIsCorrect());
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("CPlusPlusDirectReferenceWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(
                File.Exists(this.GetPath(@"Library\Library.Windows.mdproj")) ||
                File.Exists(this.GetPath(@"Library\Library.Windows.vcxproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            if (Path.DirectorySeparatorChar == '/')
            {
                _assert.Contains("libLibrary.so", consoleContents);
            }
            else
            {
                _assert.Contains("Library32.dll", consoleContents);
                _assert.Contains("Library64.dll", consoleContents);
            }

            _assert.Contains("LibraryBinding.dll", consoleContents);
        }
    }
}