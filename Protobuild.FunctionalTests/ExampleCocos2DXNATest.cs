namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExampleCocos2DXNATest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExampleCocos2DXNATest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect(string parent, string child)
        {
            this.SetupTest("ExampleCocos2DXNA", parent: parent, child: child);

            // Newer versions of Protobuild upgrade this file, but for regression testing we need the old
            // format (it's expected that the old format will be used in conjunction with old versions of
            // Protobuild, which are the version that we're regression testing with).  Copy across
            // the old version before the start of every test here.
            File.Copy(this.GetPath(@"Build\ModuleOldFormat.xml"), this.GetPath(@"Build\Module.xml"), true);
            File.Copy(this.GetPath(@"MonoGame\Build\ModuleOldFormat.xml"), this.GetPath(@"MonoGame\Build\Module.xml"), true);

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Cocos2DXNA.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Cocos2DXNA\Cocos2DXNA.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj")));

            var gameContents = this.ReadFile(@"Cocos2DXNA\Cocos2DXNA.Windows.csproj");
            var solutionContents = this.ReadFile(@"Cocos2DXNA.Windows.sln");

            _assert.Contains("Microsoft.Xna.Framework", gameContents);
            _assert.DoesNotContain("MonoGame.Framework.Windows", gameContents);
            _assert.DoesNotContain("MonoGame.Framework.Windows", solutionContents);

            this.Generate("Linux");

            _assert.True(File.Exists(this.GetPath(@"Cocos2DXNA.Linux.sln")));
            _assert.True(File.Exists(this.GetPath(@"Cocos2DXNA\Cocos2DXNA.Linux.csproj")));
            _assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Linux.csproj")));

            gameContents = this.ReadFile(@"Cocos2DXNA\Cocos2DXNA.Linux.csproj");
            solutionContents = this.ReadFile(@"Cocos2DXNA.Linux.sln");

            _assert.DoesNotContain("Microsoft.Xna.Framework", gameContents);
            _assert.Contains("MonoGame.Framework.Linux", gameContents);
            _assert.Contains("MonoGame.Framework.Linux", solutionContents);
        }
    }
}