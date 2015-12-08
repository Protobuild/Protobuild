namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class IncludeProjectPlatformSpecificWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public IncludeProjectPlatformSpecificWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("IncludeProjectPlatformSpecificWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<Link>Included Code\\Includable\\MyIncludableClass.cs</Link>", projectContents);

            this.Generate("Linux");

            _assert.True(File.Exists(this.GetPath(@"Module.Linux.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));

            projectContents = this.ReadFile(@"Console\Console.Linux.csproj");

            _assert.DoesNotContain("<Link>Included Code\\Includable\\MyIncludableClass.cs</Link>", projectContents);
        }
    }
}