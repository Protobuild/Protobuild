namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;
    
    public class CPlusPlusSWIGBindingGeneratorWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public CPlusPlusSWIGBindingGeneratorWorksTest(IAssert assert, ICategorize categorize) : base(assert)
        {
            _assert = assert;

            categorize.Method("CPlusPlusPotentialSWIGInstallation", () => GenerationIsCorrect());
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("CPlusPlusSWIGBindingGeneratorWorks");

            this.Generate("Windows");

            _assert.True(
                File.Exists(this.GetPath(@"Library\Library.Windows.mdproj")) ||
                File.Exists(this.GetPath(@"Library\Library.Windows.vcxproj")));

            if (File.Exists(this.GetPath(@"Library\Library.Windows.mdproj")))
            {
                var consoleContents = this.ReadFile(@"Library\Library.Windows.mdproj");

                _assert.Contains("swig -csharp -dllimport libLibrary util.i", consoleContents);
            }
            else if (File.Exists(this.GetPath(@"Library\Library.Windows.vcxproj")))
            {
                var consoleContents = this.ReadFile(@"Library\Library.Windows.vcxproj");

                _assert.Contains("swig.exe\" -csharp -dllimport Library util.i", consoleContents);
            }
        }
    }
}