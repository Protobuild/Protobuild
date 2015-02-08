namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ExampleCocos2DXNATest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ExampleCocos2DXNA");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Cocos2DXNA.Windows.sln")));
            Assert.True(File.Exists(this.GetPath(@"Cocos2DXNA\Cocos2DXNA.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj")));

            var gameContents = this.ReadFile(@"Cocos2DXNA\Cocos2DXNA.Windows.csproj");
            var solutionContents = this.ReadFile(@"Cocos2DXNA.Windows.sln");

            Assert.Contains("Microsoft.Xna.Framework", gameContents);
            Assert.DoesNotContain("MonoGame.Framework.Windows", gameContents);
            Assert.DoesNotContain("MonoGame.Framework.Windows", solutionContents);

            this.Generate("Linux");

            Assert.True(File.Exists(this.GetPath(@"Cocos2DXNA.Linux.sln")));
            Assert.True(File.Exists(this.GetPath(@"Cocos2DXNA\Cocos2DXNA.Linux.csproj")));
            Assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Linux.csproj")));

            gameContents = this.ReadFile(@"Cocos2DXNA\Cocos2DXNA.Linux.csproj");
            solutionContents = this.ReadFile(@"Cocos2DXNA.Linux.sln");

            Assert.DoesNotContain("Microsoft.Xna.Framework", gameContents);
            Assert.Contains("MonoGame.Framework.Linux", gameContents);
            Assert.Contains("MonoGame.Framework.Linux", solutionContents);
        }
    }
}