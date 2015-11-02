namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExampleMonoGameTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExampleMonoGameTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ExampleMonoGame");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Game\Game.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj")));

            var gameContents = this.ReadFile(@"Game\Game.Windows.csproj");
            var frameworkContents = this.ReadFile(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj");

            _assert.Contains("MonoGame.Framework.Windows", gameContents);
            _assert.Contains("SERVICE_DEFAULT;", frameworkContents);
            _assert.DoesNotContain("SERVICE_ENABLE_GL;", frameworkContents);
            _assert.Contains("MyClass.DirectX.cs", frameworkContents);
            _assert.DoesNotContain("MyClass.OpenGL.cs", frameworkContents);

            this.Generate("WindowsGL");

            _assert.True(File.Exists(this.GetPath(@"Game\Game.WindowsGL.csproj")));
            _assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.WindowsGL.csproj")));

            gameContents = this.ReadFile(@"Game\Game.WindowsGL.csproj");
            frameworkContents = this.ReadFile(@"MonoGame\MonoGame.Framework\MonoGame.Framework.WindowsGL.csproj");

            _assert.Contains("MonoGame.Framework.Windows", gameContents);
            _assert.Contains("SERVICE_DEFAULT;", frameworkContents);
            _assert.Contains("SERVICE_ENABLE_GL;", frameworkContents);
            _assert.DoesNotContain("MyClass.DirectX.cs", frameworkContents);
            _assert.Contains("MyClass.OpenGL.cs", frameworkContents);

            this.Generate("Windows", "--enable MonoGame.Framework/GLBackend");

            _assert.True(File.Exists(this.GetPath(@"Game\Game.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj")));

            gameContents = this.ReadFile(@"Game\Game.Windows.csproj");
            frameworkContents = this.ReadFile(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj");

            _assert.Contains("MonoGame.Framework.Windows", gameContents);
            _assert.Contains("SERVICE_DEFAULT;", frameworkContents);
            _assert.Contains("SERVICE_ENABLE_GL;", frameworkContents);
            _assert.DoesNotContain("MyClass.DirectX.cs", frameworkContents);
            _assert.Contains("MyClass.OpenGL.cs", frameworkContents);

            this.Generate("Windows", "--enable GLBackend");

            _assert.True(File.Exists(this.GetPath(@"Game\Game.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj")));

            gameContents = this.ReadFile(@"Game\Game.Windows.csproj");
            frameworkContents = this.ReadFile(@"MonoGame\MonoGame.Framework\MonoGame.Framework.Windows.csproj");

            _assert.Contains("MonoGame.Framework.Windows", gameContents);
            _assert.Contains("SERVICE_DEFAULT;", frameworkContents);
            _assert.Contains("SERVICE_ENABLE_GL;", frameworkContents);
            _assert.DoesNotContain("MyClass.DirectX.cs", frameworkContents);
            _assert.Contains("MyClass.OpenGL.cs", frameworkContents);
        }
    }
}