namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class IncludeProjectViaExternalWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public IncludeProjectViaExternalWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("IncludeProjectViaExternalWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath(@"Includable\Includable.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<Link>Included Code\\Includable\\MyIncludableClass.cs</Link>", projectContents);
            _assert.Contains("<FromIncludeProject>", projectContents);
            _assert.Contains("..\\Includable\\MyIncludableClass.cs", projectContents);
            _assert.DoesNotContain("<Reference Include=\"Includable\" />", projectContents);
        }
    }
}