namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class DefaultStartupProjectTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public DefaultStartupProjectTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("DefaultProjectNotSet");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));

            var solutionContents = this.ReadFile(@"Module.Windows.sln");
            var consoleAIndex = solutionContents.IndexOf("ConsoleA");
            var consoleBIndex = solutionContents.IndexOf("ConsoleB");

            _assert.NotEqual(-1, consoleAIndex);
            _assert.NotEqual(-1, consoleBIndex);
            _assert.True(consoleAIndex < consoleBIndex, "Console A must appear before Console B");

            this.SetupTest("DefaultProjectExplicitlySet");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));

            solutionContents = this.ReadFile(@"Module.Windows.sln");
            consoleAIndex = solutionContents.IndexOf("ConsoleA");
            consoleBIndex = solutionContents.IndexOf("ConsoleB");

            _assert.NotEqual(-1, consoleAIndex);
            _assert.NotEqual(-1, consoleBIndex);
            _assert.True(consoleBIndex < consoleAIndex, "Console B must appear before Console A");
        }
    }
}