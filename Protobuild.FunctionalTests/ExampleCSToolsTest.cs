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

        public void GenerationIsCorrect(string parent, string child)
        {
            this.SetupTest("ExampleCSTools", parent: parent, child: child);

            // Newer versions of Protobuild upgrade this file, but for regression testing we need the old
            // format (it's expected that the old format will be used in conjunction with old versions of
            // Protobuild, which are the version that we're regression testing with).  Copy across
            // the old version before the start of every test here.
            File.Copy(this.GetPath(@"Build\ModuleOldFormat.xml"), this.GetPath(@"Build\Module.xml"), true);
            File.Copy(this.GetPath(@"CSTools\Build\ModuleOldFormat.xml"), this.GetPath(@"CSTools\Build\Module.xml"), true);

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