namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class CheckForOverflowUnderflowPropertyWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public CheckForOverflowUnderflowPropertyWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("CheckForOverflowUnderflowPropertyWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<CheckForOverflowUnderflow>True</CheckForOverflowUnderflow>", consoleContents);
        }
    }
}