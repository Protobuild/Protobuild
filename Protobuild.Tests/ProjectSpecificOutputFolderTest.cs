namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ProjectSpecificOutputFolderTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ProjectSpecificOutputFolder");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"ConsoleA\ConsoleA.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"ConsoleB\ConsoleB.Windows.csproj")));

            var consoleAContents = this.ReadFile(@"ConsoleA\ConsoleA.Windows.csproj");
            var consoleBContents = this.ReadFile(@"ConsoleB\ConsoleB.Windows.csproj");

            Assert.Contains("<OutputPath>bin\\Windows\\AnyCPU\\Debug</OutputPath>", consoleAContents);
            Assert.DoesNotContain("<OutputPath>bin\\ConsoleA\\Windows\\AnyCPU\\Debug</OutputPath>", consoleAContents);
            Assert.Contains("<OutputPath>bin\\ConsoleB\\Windows\\AnyCPU\\Debug</OutputPath>", consoleBContents);
            Assert.DoesNotContain("<OutputPath>bin\\Windows\\AnyCPU\\Debug</OutputPath>", consoleBContents);
        }
    }
}