namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class NativeBinaryPresentTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("NativeBinaryPresent");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.Contains("<Link>All</Link>", projectContents);
            Assert.Contains("<Link>Windows.dll</Link>", projectContents);
            Assert.DoesNotContain("<Link>Linux.so</Link>", projectContents);
            Assert.Contains("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>", projectContents);
            Assert.Contains(@"<None Include=""..\ThirdParty\All"">", projectContents);
            Assert.Contains(@"<None Include=""..\ThirdParty\Windows.dll"">", projectContents);
            Assert.DoesNotContain(@"<None Include=""..\ThirdParty\Linux.so"">", projectContents);

            this.Generate("Linux");

            Assert.True(File.Exists(this.GetPath(@"Module.Linux.sln")));
            Assert.True(File.Exists(this.GetPath(@"Console\Console.Linux.csproj")));

            projectContents = this.ReadFile(@"Console\Console.Linux.csproj");

            Assert.Contains("<Link>All</Link>", projectContents);
            Assert.DoesNotContain("<Link>Windows.dll</Link>", projectContents);
            Assert.Contains("<Link>Linux.so</Link>", projectContents);
            Assert.Contains("<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>", projectContents);
            Assert.Contains(@"<None Include=""..\ThirdParty\All"">", projectContents);
            Assert.DoesNotContain(@"<None Include=""..\ThirdParty\Windows.dll"">", projectContents);
            Assert.Contains(@"<None Include=""..\ThirdParty\Linux.so"">", projectContents);
        }
    }
}