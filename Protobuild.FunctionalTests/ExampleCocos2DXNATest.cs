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

        public void GenerationIsCorrect()
        {
            this.SetupTest("ExampleCocos2DXNA");

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