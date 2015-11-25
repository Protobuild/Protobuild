namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class IncludeProjectAppliesToWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public IncludeProjectAppliesToWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("IncludeProjectAppliesToWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<Link>Included Code\\Submodule/IncludableA\\MyIncludableClass.cs</Link>", projectContents);
        }
    }
}