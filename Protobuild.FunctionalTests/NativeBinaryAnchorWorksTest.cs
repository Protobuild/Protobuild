namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class NativeBinaryAnchorWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public NativeBinaryAnchorWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("NativeBinaryAnchorWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<Link>Anchor/All</Link>", projectContents);
        }
    }
}