namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ExampleMonoGameTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ExampleMonoGame");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Game\Game.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj")));

            var gameContents = this.ReadFile(@"Game\Game.Windows.csproj");
            var frameworkContents = this.ReadFile(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj");

            Assert.Contains("MonoGame.Framework.Windows", gameContents);
            Assert.Contains("SERVICE_DEFAULT;", frameworkContents);
            Assert.DoesNotContain("SERVICE_ENABLE_GL;", frameworkContents);

            this.Generate("WindowsGL");

            Assert.True(File.Exists(this.GetPath(@"Game\Game.WindowsGL.csproj")));
            Assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.WindowsGL.csproj")));

            gameContents = this.ReadFile(@"Game\Game.WindowsGL.csproj");
            frameworkContents = this.ReadFile(@"MonoGame\MonoGame.Framework\MonoGame.Framework.WindowsGL.csproj");

            Assert.Contains("MonoGame.Framework.Windows", gameContents);
            Assert.Contains("SERVICE_DEFAULT;", frameworkContents);
            Assert.Contains("SERVICE_ENABLE_GL;", frameworkContents);

            this.Generate("Windows", "--enable MonoGame.Framework/GLBackend");

            Assert.True(File.Exists(this.GetPath(@"Game\Game.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj")));

            gameContents = this.ReadFile(@"Game\Game.Windows.csproj");
            frameworkContents = this.ReadFile(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj");

            Assert.Contains("MonoGame.Framework.Windows", gameContents);
            Assert.Contains("SERVICE_DEFAULT;", frameworkContents);
            Assert.Contains("SERVICE_ENABLE_GL;", frameworkContents);

            this.Generate("Windows", "--enable GLBackend");

            Assert.True(File.Exists(this.GetPath(@"Game\Game.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj")));

            gameContents = this.ReadFile(@"Game\Game.Windows.csproj");
            frameworkContents = this.ReadFile(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj");

            Assert.Contains("MonoGame.Framework.Windows", gameContents);
            Assert.Contains("SERVICE_DEFAULT;", frameworkContents);
            Assert.Contains("SERVICE_ENABLE_GL;", frameworkContents);
        }
    }
}