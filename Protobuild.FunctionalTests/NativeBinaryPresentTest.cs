namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class NativeBinaryPresentTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public NativeBinaryPresentTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("NativeBinaryPresent");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<Link>All</Link>", projectContents);
            _assert.Contains("<Link>Windows.dll</Link>", projectContents);
            _assert.DoesNotContain("<Link>Linux.so</Link>", projectContents);
            _assert.Contains("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>", projectContents);
            _assert.Contains(@"<None Include=""..\ThirdParty\All"">", projectContents);
            _assert.Contains(@"<None Include=""..\ThirdParty\Windows.dll"">", projectContents);
            _assert.DoesNotContain(@"<None Include=""..\ThirdParty\Linux.so"">", projectContents);

            this.Generate("Linux");

            _assert.True(File.Exists(this.GetPath(@"Module.Linux.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));

            projectContents = this.ReadFile(@"Console\Console.Linux.csproj");

            _assert.Contains("<Link>All</Link>", projectContents);
            _assert.DoesNotContain("<Link>Windows.dll</Link>", projectContents);
            _assert.Contains("<Link>Linux.so</Link>", projectContents);
            _assert.Contains("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>", projectContents);
            _assert.Contains(@"<None Include=""..\ThirdParty\All"">", projectContents);
            _assert.DoesNotContain(@"<None Include=""..\ThirdParty\Windows.dll"">", projectContents);
            _assert.Contains(@"<None Include=""..\ThirdParty\Linux.so"">", projectContents);
        }
    }
}