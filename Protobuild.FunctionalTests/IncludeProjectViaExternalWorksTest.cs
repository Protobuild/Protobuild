namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class IncludeProjectViaExternalWorksTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("IncludeProjectViaExternalWorks");

            this.Generate("Windows");

            Assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath(@"Includable\Includable.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            Assert.Contains("<Link>Included Code\\Includable\\MyIncludableClass.cs</Link>", projectContents);
            Assert.Contains("<FromIncludeProject>", projectContents);
            Assert.Contains("..\\Includable\\MyIncludableClass.cs", projectContents);
            Assert.DoesNotContain("<Reference Include=\"Includable\" />", projectContents);
        }
    }
}