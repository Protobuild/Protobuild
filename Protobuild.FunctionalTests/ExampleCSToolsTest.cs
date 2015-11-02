namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExampleCSToolsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExampleCSToolsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ExampleCSTools");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Game\Game.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"CSTools\Tool\Tool.Windows.csproj")));

            var gameContents = this.ReadFile(@"Game\Game.Windows.csproj");
            var solutionContents = this.ReadFile(@"Game.Windows.sln");

            _assert.DoesNotContain("Tool.Windows.csproj", gameContents);
            _assert.Contains("Game.Windows.csproj", solutionContents);
            _assert.Contains("Tool.Windows.csproj", solutionContents);
        }
    }
}