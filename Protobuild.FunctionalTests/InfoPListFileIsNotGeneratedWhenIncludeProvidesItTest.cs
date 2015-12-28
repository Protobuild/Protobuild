namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

	public class InfoPListFileIsNotGeneratedWhenIncludeProvidesItTest : ProtobuildTest
    {
        private readonly IAssert _assert;

		public InfoPListFileIsNotGeneratedWhenIncludeProvidesItTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
			this.SetupTest("InfoPListFileIsNotGeneratedWhenIncludeProvidesIt");

            var infoPList = this.GetPath(@"Console\Info.plist");
            if (File.Exists(infoPList))
            {
                File.Delete(infoPList);
            }

            this.Generate("MacOS", hostPlatform: "MacOS");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.MacOS.csproj")));
			_assert.False(File.Exists(infoPList));

            var consoleContents = this.ReadFile(@"Console\Console.MacOS.csproj");
			_assert.DoesNotContain("<None Include=\"Info.plist\"", consoleContents);
        }
    }
}