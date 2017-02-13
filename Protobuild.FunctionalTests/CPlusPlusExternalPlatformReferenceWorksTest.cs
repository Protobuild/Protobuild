namespace Protobuild.Tests
{
    using System;
    using System.IO;
    using Prototest.Library.Version1;
    
    public class CPlusPlusExternalPlatformReferenceWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public CPlusPlusExternalPlatformReferenceWorksTest(IAssert assert, ICategorize categorize) : base(assert)
        {
            _assert = assert;

            categorize.Method("CPlusPlusPotentialSWIGInstallation", () => GenerationIsCorrect());
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("CPlusPlusExternalPlatformReferenceWorks");

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

            this.Generate("Linux");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));
            _assert.True(
                File.Exists(this.GetPath(@"Library\Library.Linux.mdproj")) ||
                File.Exists(this.GetPath(@"Library\Library.Linux.vcxproj")));

            consoleContents = this.ReadFile(@"Console\Console.Linux.csproj");

            if (Path.DirectorySeparatorChar == '/')
            {
                _assert.DoesNotContain("libLibrary.so", consoleContents);
            }
            else
            {
                _assert.DoesNotContain("Library32.dll", consoleContents);
                _assert.DoesNotContain("Library64.dll", consoleContents);
            }

            _assert.DoesNotContain("LibraryBinding.dll", consoleContents);
        }
    }
}