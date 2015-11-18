namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class InfoPListFileIsGeneratedTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public InfoPListFileIsGeneratedTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("InfoPListFileIsGenerated");

            var infoPList = this.GetPath(@"Console\Info.plist");
            if (File.Exists(infoPList))
            {
                File.Delete(infoPList);
            }

            this.Generate("MacOS", hostPlatform: "MacOS");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.MacOS.csproj")));
            _assert.True(File.Exists(infoPList));

            var consoleContents = this.ReadFile(@"Console\Console.MacOS.csproj");
            _assert.Contains("<None Include=\"Info.plist\"", consoleContents);
        }
    }
}