namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class IncludeProjectViaSubmoduleWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("IncludeProjectViaSubmoduleWorks");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath(@"Submodule\IncludableA\IncludableA.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath(@"Submodule\IncludableB\IncludableB.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.Contains("<Link>Included Code\\Submodule\\IncludableA\\MyIncludableClass.cs</Link>", projectContents);
            Assert.Contains("<Link>Included Code\\Submodule\\IncludableB\\MyIncludableClass.cs</Link>", projectContents);
            Assert.Contains("<FromIncludeProject>", projectContents);
            Assert.Contains("..\\Submodule\\IncludableA\\MyIncludableClass.cs", projectContents);
            Assert.Contains("..\\Submodule\\IncludableB\\MyIncludableClass.cs", projectContents);
            Assert.DoesNotContain("<Reference Include=\"IncludableA\" />", projectContents);
            Assert.DoesNotContain("<Reference Include=\"IncludableB\" />", projectContents);
            Assert.DoesNotContain("<Reference Include=\"External\" />", projectContents);
        }
    }
}