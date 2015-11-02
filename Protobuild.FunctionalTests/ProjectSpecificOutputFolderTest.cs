namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ProjectSpecificOutputFolderTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ProjectSpecificOutputFolderTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ProjectSpecificOutputFolder");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"ConsoleA\ConsoleA.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"ConsoleB\ConsoleB.Windows.csproj")));

            var consoleAContents = this.ReadFile(@"ConsoleA\ConsoleA.Windows.csproj");
            var consoleBContents = this.ReadFile(@"ConsoleB\ConsoleB.Windows.csproj");

            _assert.Contains("<OutputPath>bin\\Windows\\AnyCPU\\Debug</OutputPath>", consoleAContents);
            _assert.DoesNotContain("<OutputPath>bin\\ConsoleA\\Windows\\AnyCPU\\Debug</OutputPath>", consoleAContents);
            _assert.Contains("<OutputPath>bin\\ConsoleB\\Windows\\AnyCPU\\Debug</OutputPath>", consoleBContents);
            _assert.DoesNotContain("<OutputPath>bin\\Windows\\AnyCPU\\Debug</OutputPath>", consoleBContents);
        }
    }
}