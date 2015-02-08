namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageLocationParentAndSubmoduleSamePackageTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageLocationParentAndSubmoduleSamePackage");

            var src = this.SetupSrcPackage();

            // Make sure the Package directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Package")))
            {
                Directory.Delete(this.GetPath("Package"), true);
            }

            this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

            Assert.True(File.Exists(this.GetPath("Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath("Submodule\\Package\\.redirect")));
            Assert.True(File.Exists(this.GetPath("Submodule\\Library\\Library.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath("Console\\Console.Windows.csproj")));

            var consoleContents = this.ReadFile("Console\\Console.Windows.csproj");
            var libraryContents = this.ReadFile("Submodule\\Library\\Library.Windows.csproj");

            Assert.Contains(
                @"Include=""..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                consoleContents);
            Assert.Contains(
                @"Include=""..\..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                libraryContents);
        }
    }
}