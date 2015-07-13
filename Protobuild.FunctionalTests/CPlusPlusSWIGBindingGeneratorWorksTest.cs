namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    [Collection("CPlusPlusPotentialSWIGInstallation")]
    public class CPlusPlusSWIGBindingGeneratorWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("CPlusPlusSWIGBindingGeneratorWorks");

            this.Generate("Windows");

            Assert.True(
                File.Exists(this.GetPath(@"Library\Library.Windows.cproj")) ||
                File.Exists(this.GetPath(@"Library\Library.Windows.vcxproj")));

            if (File.Exists(this.GetPath(@"Library\Library.Windows.cproj")))
            {
                var consoleContents = this.ReadFile(@"Library\Library.Windows.cproj");

                Assert.Contains("swig -csharp -dllimport libLibrary util.i", consoleContents);
            }
            else if (File.Exists(this.GetPath(@"Library\Library.Windows.vcxproj")))
            {
                var consoleContents = this.ReadFile(@"Library\Library.Windows.vcxproj");

                Assert.Contains("swig.exe\" -csharp -dllimport Library util.i", consoleContents);
            }
        }
    }
}