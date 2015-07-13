namespace Protobuild.Tests
{
    using System;
    using System.IO;
    using Xunit;

    [Collection("CPlusPlusPotentialSWIGInstallation")]
    public class CPlusPlusDirectReferenceWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("CPlusPlusDirectReferenceWorks");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(
                File.Exists(this.GetPath(@"Library\Library.Windows.cproj")) ||
                File.Exists(this.GetPath(@"Library\Library.Windows.vcxproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            if (Path.DirectorySeparatorChar == '/')
            {
                Assert.Contains("libLibrary.so", consoleContents);
            }
            else
            {
                Assert.Contains("Library32.dll", consoleContents);
                Assert.Contains("Library64.dll", consoleContents);
            }

            Assert.Contains("LibraryBinding.dll", consoleContents);
        }
    }
}