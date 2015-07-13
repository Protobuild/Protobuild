namespace Protobuild.Tests
{
    using System;
    using System.IO;
    using Xunit;

    [Collection("CPlusPlusPotentialSWIGInstallation")]
    public class CPlusPlusExternalPlatformReferenceWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("CPlusPlusExternalPlatformReferenceWorks");

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

            this.Generate("Linux");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));
            Assert.True(
                File.Exists(this.GetPath(@"Library\Library.Linux.cproj")) ||
                File.Exists(this.GetPath(@"Library\Library.Linux.vcxproj")));

            consoleContents = this.ReadFile(@"Console\Console.Linux.csproj");

            if (Path.DirectorySeparatorChar == '/')
            {
                Assert.DoesNotContain("libLibrary.so", consoleContents);
            }
            else
            {
                Assert.DoesNotContain("Library32.dll", consoleContents);
                Assert.DoesNotContain("Library64.dll", consoleContents);
            }

            Assert.DoesNotContain("LibraryBinding.dll", consoleContents);
        }
    }
}