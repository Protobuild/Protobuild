namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class IncludeProjectViaSubmoduleWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public IncludeProjectViaSubmoduleWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("IncludeProjectViaSubmoduleWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath(@"Submodule\IncludableA\IncludableA.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath(@"Submodule\IncludableB\IncludableB.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<Link>Included Code\\Submodule/IncludableA\\MyIncludableClass.cs</Link>", projectContents);
            _assert.Contains("<Link>Included Code\\Submodule/IncludableB\\MyIncludableClass.cs</Link>", projectContents);
            _assert.Contains("<FromIncludeProject>", projectContents);
            _assert.Contains("..\\Submodule\\IncludableA\\MyIncludableClass.cs", projectContents);
            _assert.Contains("..\\Submodule\\IncludableB\\MyIncludableClass.cs", projectContents);
            _assert.DoesNotContain("<Reference Include=\"IncludableA\" />", projectContents);
            _assert.DoesNotContain("<Reference Include=\"IncludableB\" />", projectContents);
            _assert.DoesNotContain("<Reference Include=\"External\" />", projectContents);
        }
    }
}